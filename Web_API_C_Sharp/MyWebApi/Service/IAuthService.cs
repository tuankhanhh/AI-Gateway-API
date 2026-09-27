using System.Threading.Tasks;
using MyWebApi.DTO.Request;
using MyWebApi.DTO.Response;

namespace MyWebApi.Service
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterDto request);
        Task<LoginResponseDto> LoginAsync(LoginDto request);
        Task<TokenResponseDto> RefreshAsync(RefreshTokenDto request);
        Task LogoutAsync(string refreshToken);
        Task<CurrentUserDto> GetCurrentUserAsync(string userId);
    }
}
