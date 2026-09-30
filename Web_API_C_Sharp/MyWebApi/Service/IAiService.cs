using System.Threading.Tasks;
using MyWebApi.DTO.AI;

namespace MyWebApi.Service
{
    public interface IAiService
    {
        Task<ChatResponseDto> ChatAsync(int userId, ChatRequestDto request);
        Task<AnalyzeResponseDto> AnalyzeAsync(int userId, AnalyzeRequestDto request);
    }
}
