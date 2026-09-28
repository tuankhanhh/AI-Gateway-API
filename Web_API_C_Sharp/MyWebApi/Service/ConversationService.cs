using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyWebApi.DTO.Conversation;
using MyWebApi.Models;
using MyWebApi.Repository;

namespace MyWebApi.Service
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IMessageRepository _messageRepository;

        public ConversationService(IConversationRepository conversationRepository, IMessageRepository messageRepository)
        {
            _conversationRepository = conversationRepository;
            _messageRepository = messageRepository;
        }

        public async Task<ConversationResponseDto> CreateAsync(int userId, CreateConversationDto request)
        {
            var conversation = new Conversation
            {
                UserId = userId,
                Title = request.Title,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _conversationRepository.AddAsync(conversation);

            return new ConversationResponseDto
            {
                Id = conversation.Id,
                Title = conversation.Title ?? string.Empty,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt
            };
        }

        public async Task<List<ConversationResponseDto>> GetMyConversationsAsync(int userId)
        {
            var conversations = await _conversationRepository.GetByUserIdAsync(userId);
            
            return conversations.Select(c => new ConversationResponseDto
            {
                Id = c.Id,
                Title = c.Title ?? string.Empty,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();
        }

        public async Task<ConversationResponseDto?> GetByIdAsync(int userId, int conversationId)
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            
            if (conversation == null || conversation.UserId != userId)
            {
                return null;
            }

            return new ConversationResponseDto
            {
                Id = conversation.Id,
                Title = conversation.Title ?? string.Empty,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt
            };
        }

        public async Task<bool> DeleteAsync(int userId, int conversationId)
        {
            var conversation = await _conversationRepository.GetByIdAsync(conversationId);
            
            if (conversation == null || conversation.UserId != userId)
            {
                return false;
            }

            // Xóa messages trước (nếu không có cascade delete)
            await _messageRepository.DeleteByConversationIdAsync(conversationId);
            
            // Sau đó xóa conversation
            await _conversationRepository.DeleteAsync(conversation);
            return true;
        }
    }
}
