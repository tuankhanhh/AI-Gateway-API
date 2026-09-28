using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyWebApi.DTO.Conversation;
using MyWebApi.Models;
using MyWebApi.Repository;

namespace MyWebApi.Service
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;

        public MessageService(IMessageRepository messageRepository, IConversationRepository conversationRepository)
        {
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
        }

        public async Task<MessageResponseDto?> CreateUserMessageAsync(int userId, int conversationId, MessageCreateDto request)
        {
            // Kiểm tra ownership
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null || conversation.UserId != userId)
            {
                return null;
            }

            var message = new Message
            {
                ConversationId = conversationId,
                Role = "user",
                Content = request.Content,
                Model = null,
                CreatedAt = DateTime.UtcNow
            };

            await _messageRepository.AddAsync(message);

            // Cập nhật UpdatedAt cho Conversation
            conversation.UpdatedAt = DateTime.UtcNow;
            await _conversationRepository.UpdateAsync(conversation);

            return new MessageResponseDto
            {
                Id = message.Id,
                Role = message.Role,
                Content = message.Content,
                Model = message.Model,
                CreatedAt = message.CreatedAt
            };
        }

        public async Task<List<MessageResponseDto>?> GetMessagesAsync(int userId, int conversationId)
        {
            // Kiểm tra ownership
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            if (conversation == null || conversation.UserId != userId)
            {
                return null;
            }

            var messages = await _messageRepository.GetByConversationIdAsync(conversationId);
            
            return messages.Select(m => new MessageResponseDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                Model = m.Model,
                CreatedAt = m.CreatedAt
            }).ToList();
        }
    }
}
