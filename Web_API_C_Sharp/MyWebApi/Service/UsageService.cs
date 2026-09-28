using System;
using System.Linq;
using System.Threading.Tasks;
using MyWebApi.DTO.Usage;
using MyWebApi.Repository;

namespace MyWebApi.Service
{
    public class UsageService : IUsageService
    {
        private readonly IAiRequestLogRepository _logRepository;

        public UsageService(IAiRequestLogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public async Task<UsageResponseDto> GetMyUsageAsync(int userId)
        {
            var logs = await _logRepository.GetByUserIdAsync(userId);
            
            int totalRequests = logs.Count;
            if (totalRequests == 0)
            {
                return new UsageResponseDto();
            }

            int failedRequests = logs.Count(l => l.Status == "Failed");
            
            var validLatencyLogs = logs.Where(l => l.LatencyMs.HasValue).ToList();
            double avgLatency = validLatencyLogs.Any() ? validLatencyLogs.Average(l => l.LatencyMs.Value) : 0;
            
            int inputTokens = logs.Sum(l => l.InputTokens ?? 0);
            int outputTokens = logs.Sum(l => l.OutputTokens ?? 0);

            return new UsageResponseDto
            {
                Requests = totalRequests,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                TotalTokens = inputTokens + outputTokens,
                AverageLatencyMs = Math.Round(avgLatency, 2),
                ErrorRate = Math.Round((double)failedRequests / totalRequests, 4)
            };
        }
    }
}
