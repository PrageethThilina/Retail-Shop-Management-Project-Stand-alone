using RetailShop.Core.Common;
using RetailShop.Core.Models;

namespace RetailShop.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> AuthenticateAsync(string username, string plainPassword);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<PagedResult<User>> GetPagedAsync(PagedRequest request);
    Task<int> CreateAsync(User user, string plainPassword);
    Task<bool> UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
    Task<bool> UsernameExistsAsync(string username, int? excludeId = null);
}
