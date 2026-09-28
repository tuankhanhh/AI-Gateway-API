using System.Collections.Generic;
using System.Threading.Tasks;
using MyWebApi.Models;

namespace MyWebApi.Repository
{
    public interface IMessageRepository
    {
        Task<List<Message>> GetByConversationIdAsync(int conversationId);
        Task AddAsync(Message message);
        Task DeleteByConversationIdAsync(int conversationId);
    }
}
