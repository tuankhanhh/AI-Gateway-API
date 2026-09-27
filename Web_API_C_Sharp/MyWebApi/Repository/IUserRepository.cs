using System.Collections.Generic;
using System.Threading.Tasks;
using MyWebApi.Models;

namespace MyWebApi.Repository
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int userId);
        Task<bool> ExistsByEmailAsync(string email);
        Task AddAsync(User user);
        Task<List<string>> GetRolesAsync(int userId);
        Task<Role?> GetRoleByNameAsync(string roleName);
    }
}
