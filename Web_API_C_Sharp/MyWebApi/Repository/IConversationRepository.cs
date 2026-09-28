using System.Collections.Generic;
using System.Threading.Tasks;
using MyWebApi.Models;

namespace MyWebApi.Repository
{
    public interface IConversationRepository
    {
        Task<List<Conversation>> GetByUserIdAsync(int userId);
        Task<Conversation?> GetByIdAsync(int conversationId);
        Task AddAsync(Conversation conversation);
        Task DeleteAsync(Conversation conversation);
        Task UpdateAsync(Conversation conversation);
    }
}
