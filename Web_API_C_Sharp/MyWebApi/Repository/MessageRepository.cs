using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyWebApi.Models;

namespace MyWebApi.Repository
{
    public class MessageRepository : IMessageRepository
    {
        private readonly ApplicationDbContext _context;

        public MessageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Message>> GetByConversationIdAsync(int conversationId)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(Message message)
        {
            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteByConversationIdAsync(int conversationId)
        {
            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .ToListAsync();
            
            if (messages.Any())
            {
                _context.Messages.RemoveRange(messages);
                await _context.SaveChangesAsync();
            }
        }
    }
}
