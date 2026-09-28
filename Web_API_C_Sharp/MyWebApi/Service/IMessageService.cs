using System.Collections.Generic;
using System.Threading.Tasks;
using MyWebApi.DTO.Conversation;

namespace MyWebApi.Service
{
    public interface IMessageService
    {
        Task<MessageResponseDto?> CreateUserMessageAsync(int userId, int conversationId, MessageCreateDto request);
        Task<List<MessageResponseDto>?> GetMessagesAsync(int userId, int conversationId);
    }
}
