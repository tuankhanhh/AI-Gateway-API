using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using MyWebApi.DTO.AI;
using MyWebApi.Models;
using MyWebApi.Providers.Interfaces;
using MyWebApi.Repository;
using MyWebApi.Exceptions;

namespace MyWebApi.Service
{
    public class AiService : IAiService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly ILLMProvider _llmProvider;
        private readonly IAiRequestLogRepository _aiRequestLogRepository;
        private readonly IMemoryCache _cache;

        public AiService(
            IConversationRepository conversationRepository,
            IMessageRepository messageRepository,
            ILLMProvider llmProvider,
            IAiRequestLogRepository aiRequestLogRepository,
            IMemoryCache cache)
        {
            _conversationRepository = conversationRepository;
            _messageRepository = messageRepository;
            _llmProvider = llmProvider;
            _aiRequestLogRepository = aiRequestLogRepository;
            _cache = cache;
        }

        public async Task<ChatResponseDto> ChatAsync(int userId, ChatRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                throw new ArgumentException("Message cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(request.Model))
            {
                throw new ArgumentException("Model cannot be empty.");
            }

            // Rate Limit Logic
            var rateLimitKey = $"RateLimit_AI_{userId}";
            var requestCount = _cache.GetOrCreate(rateLimitKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return 0;
            });

            if (requestCount >= 20)
            {
                throw new RateLimitException("Too many AI requests");
            }

            _cache.Set(rateLimitKey, requestCount + 1, TimeSpan.FromMinutes(1));

            Conversation conversation;
            List<Message> previousMessages = new List<Message>();

            if (request.ConversationId.HasValue)
            {
                var existingConv = await _conversationRepository.GetByIdAsync(request.ConversationId.Value);
                if (existingConv == null)
                {
                    throw new KeyNotFoundException("Conversation not found.");
                }

                if (existingConv.UserId != userId)
                {
                    throw new UnauthorizedAccessException("You do not own this conversation.");
                }

                conversation = existingConv;
                previousMessages = await _messageRepository.GetByConversationIdAsync(conversation.Id);
            }
            else
            {
                conversation = new Conversation
                {
                    UserId = userId,
                    Title = request.Message.Length > 50 ? request.Message.Substring(0, 50) + "..." : request.Message,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _conversationRepository.AddAsync(conversation);
            }

            // Save user message
            var userMessage = new Message
            {
                ConversationId = conversation.Id,
                Role = "user",
                Content = request.Message,
                Model = null,
                CreatedAt = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(userMessage);

            // Build LLM Request
            var llmRequest = new LLMRequest
            {
                Model = request.Model,
                Messages = previousMessages.Select(m => new LLMMessage
                {
                    Role = m.Role,
                    Content = m.Content
                }).ToList()
            };

            llmRequest.Messages.Add(new LLMMessage
            {
                Role = "user",
                Content = request.Message
            });

            // Call provider
            var providerName = _llmProvider.GetType().Name.Replace("Provider", "");
            var stopwatch = Stopwatch.StartNew();
            LLMResponse llmResponse;
            try
            {
                llmResponse = await _llmProvider.ChatAsync(llmRequest);
                stopwatch.Stop();
            }
            catch (AiProviderException ex)
            {
                stopwatch.Stop();
                var failedLog = new AiRequestLog
                {
                    UserId = userId,
                    ConversationId = conversation.Id,
                    Provider = providerName,
                    Model = request.Model,
                    RequestedAt = DateTime.UtcNow,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    InputTokens = 0,
                    OutputTokens = 0,
                    Status = "Failed",
                    ErrorCode = ex.ErrorCode
                };
                await _aiRequestLogRepository.AddAsync(failedLog);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var failedLog = new AiRequestLog
                {
                    UserId = userId,
                    ConversationId = conversation.Id,
                    Provider = providerName,
                    Model = request.Model,
                    RequestedAt = DateTime.UtcNow,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    InputTokens = 0,
                    OutputTokens = 0,
                    Status = "Failed",
                    ErrorCode = "INTERNAL_SERVER_ERROR"
                };
                await _aiRequestLogRepository.AddAsync(failedLog);
                throw new AiProviderException(500, "PROVIDER_INTERNAL_ERROR", "An unexpected error occurred while communicating with provider.");
            }

            // Success log
            var successLog = new AiRequestLog
            {
                UserId = userId,
                ConversationId = conversation.Id,
                Provider = providerName,
                Model = request.Model,
                RequestedAt = DateTime.UtcNow,
                LatencyMs = stopwatch.ElapsedMilliseconds,
                InputTokens = llmResponse.InputTokens,
                OutputTokens = llmResponse.OutputTokens,
                Status = "Success"
            };
            await _aiRequestLogRepository.AddAsync(successLog);

            // Save assistant message
            var assistantMessage = new Message
            {
                ConversationId = conversation.Id,
                Role = "assistant",
                Content = llmResponse.Content,
                Model = request.Model,
                CreatedAt = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(assistantMessage);

            // Update conversation
            conversation.UpdatedAt = DateTime.UtcNow;
            await _conversationRepository.UpdateAsync(conversation);

            return new ChatResponseDto
            {
                ConversationId = conversation.Id,
                Model = request.Model,
                Message = llmResponse.Content,
                Usage = new TokenUsageDto
                {
                    InputTokens = llmResponse.InputTokens,
                    OutputTokens = llmResponse.OutputTokens,
                    TotalTokens = llmResponse.InputTokens + llmResponse.OutputTokens
                }
            };
        }
    }
}
