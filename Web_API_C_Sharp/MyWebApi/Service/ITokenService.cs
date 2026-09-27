using System.Collections.Generic;
using MyWebApi.Models;

namespace MyWebApi.Service
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user, List<string> roles);
        string GenerateRefreshToken();
        string HashRefreshToken(string refreshToken);
    }
}
