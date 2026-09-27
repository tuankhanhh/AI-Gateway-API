using System;
using System.Threading.Tasks;
using MyWebApi.Models;

namespace MyWebApi.Repository
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken refreshToken);
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task RevokeAsync(RefreshToken refreshToken);
        Task RevokeFamilyAsync(Guid familyId);
    }
}
