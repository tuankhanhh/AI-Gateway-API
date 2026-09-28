using System.Threading.Tasks;
using MyWebApi.DTO.AI;

namespace MyWebApi.Providers.Interfaces
{
    public interface ILLMProvider
    {
        Task<LLMResponse> ChatAsync(LLMRequest request);
    }
}
