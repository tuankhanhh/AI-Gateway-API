using System.Collections.Generic;
using System.Threading.Tasks;
using MyWebApi.DTO.Conversation;

namespace MyWebApi.Service
{
    public interface IConversationService
    {
        Task<ConversationResponseDto> CreateAsync(int userId, CreateConversationDto request);
        Task<List<ConversationResponseDto>> GetMyConversationsAsync(int userId);
        Task<ConversationResponseDto?> GetByIdAsync(int userId, int conversationId);
        Task<bool> DeleteAsync(int userId, int conversationId);
    }
}
