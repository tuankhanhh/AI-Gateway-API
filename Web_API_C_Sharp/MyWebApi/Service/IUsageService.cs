using System.Threading.Tasks;
using MyWebApi.DTO.Usage;

namespace MyWebApi.Service
{
    public interface IUsageService
    {
        Task<UsageResponseDto> GetMyUsageAsync(int userId);
    }
}
