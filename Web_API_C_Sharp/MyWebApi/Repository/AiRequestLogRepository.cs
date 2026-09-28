using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyWebApi.Models;

namespace MyWebApi.Repository
{
    public class AiRequestLogRepository : IAiRequestLogRepository
    {
        private readonly ApplicationDbContext _context;

        public AiRequestLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AiRequestLog log)
        {
            await _context.AiRequestLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AiRequestLog>> GetByUserIdAsync(int userId)
        {
            return await _context.AiRequestLogs
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.RequestedAt)
                .ToListAsync();
        }
    }
}
