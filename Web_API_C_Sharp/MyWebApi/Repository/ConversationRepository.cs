using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyWebApi.Models;

namespace MyWebApi.Repository
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly ApplicationDbContext _context;

        public ConversationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Conversation>> GetByUserIdAsync(int userId)
        {
            return await _context.Conversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }

        public async Task<Conversation?> GetByIdAsync(int conversationId)
        {
            return await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);
        }

        public async Task AddAsync(Conversation conversation)
        {
            await _context.Conversations.AddAsync(conversation);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Conversation conversation)
        {
            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Conversation conversation)
        {
            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();
        }
    }
}
