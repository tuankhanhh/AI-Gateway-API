using System;
using System.Threading.Tasks;
using MyWebApi.DTO.Request;
using MyWebApi.DTO.Response;
using MyWebApi.Models;
using MyWebApi.Repository;

namespace MyWebApi.Service
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordService passwordService,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordService = passwordService;
            _tokenService = tokenService;
        }

        public async Task RegisterAsync(RegisterDto request)
        {
            if (await _userRepository.ExistsByEmailAsync(request.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            var user = new User
            {
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = _passwordService.HashPassword(request.Password),
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            var userRole = await _userRepository.GetRoleByNameAsync("User");
            if (userRole != null)
            {
                user.Roles.Add(userRole);
            }

            await _userRepository.AddAsync(user);
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            var roles = await _userRepository.GetRolesAsync(user.Id);
            
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenHash = _tokenService.HashRefreshToken(refreshToken);

            var familyId = Guid.NewGuid();

            var tokenEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                FamilyId = familyId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _refreshTokenRepository.AddAsync(tokenEntity);

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 15 * 60
            };
        }

        public async Task<TokenResponseDto> RefreshAsync(RefreshTokenDto request)
        {
            var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
            var tokenEntity = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (tokenEntity == null || tokenEntity.RevokedAt != null || tokenEntity.ExpiresAt < DateTime.UtcNow)
            {
                if (tokenEntity != null && tokenEntity.RevokedAt != null)
                {
                    await _refreshTokenRepository.RevokeFamilyAsync(tokenEntity.FamilyId);
                }
                throw new UnauthorizedAccessException("Invalid or expired refresh token");
            }

            await _refreshTokenRepository.RevokeAsync(tokenEntity);

            var user = await _userRepository.GetByIdAsync(tokenEntity.UserId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            var roles = await _userRepository.GetRolesAsync(user.Id);
            
            var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var newRefreshTokenHash = _tokenService.HashRefreshToken(newRefreshToken);

            var newTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newRefreshTokenHash,
                FamilyId = tokenEntity.FamilyId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _refreshTokenRepository.AddAsync(newTokenEntity);

            return new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = 15 * 60
            };
        }

        public async Task LogoutAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return;

            var tokenHash = _tokenService.HashRefreshToken(refreshToken);
            var tokenEntity = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (tokenEntity != null && tokenEntity.RevokedAt == null)
            {
                await _refreshTokenRepository.RevokeAsync(tokenEntity);
            }
        }

        public async Task<CurrentUserDto> GetCurrentUserAsync(string userIdStr)
        {
            if (!int.TryParse(userIdStr, out int userId))
            {
                throw new ArgumentException("Invalid user ID");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            var roles = await _userRepository.GetRolesAsync(user.Id);
            var role = roles.Count > 0 ? roles[0] : "User";

            return new CurrentUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = role
            };
        }
    }
}
