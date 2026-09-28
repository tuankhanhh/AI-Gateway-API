using System.Collections.Generic;
using System.Threading.Tasks;
using MyWebApi.Models;

namespace MyWebApi.Repository
{
    public interface IAiRequestLogRepository
    {
        Task AddAsync(AiRequestLog log);
        Task<List<AiRequestLog>> GetByUserIdAsync(int userId);
    }
}
