using RetailShop.Core.Enums;
using RetailShop.Core.Models;

namespace RetailShop.Core.Security;

public class UserSession
{
    private static readonly Lazy<UserSession> _instance = new(() => new UserSession());
    public static UserSession Current => _instance.Value;

    public int? UserId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public UserRole? Role { get; private set; }
    public DateTime? LoginTime { get; private set; }

    public bool IsAuthenticated => UserId.HasValue && Role.HasValue;

    public void StartSession(User user)
    {
        UserId = user.Id;
        Username = user.Username;
        FullName = user.FullName;
        Role = user.Role;
        LoginTime = DateTime.UtcNow;
    }

    public void ClearSession()
    {
        UserId = null;
        Username = string.Empty;
        FullName = string.Empty;
        Role = null;
        LoginTime = null;
    }

    public bool IsInRole(UserRole requiredRole)
    {
        return Role.HasValue && Role.Value == requiredRole;
    }

    public bool HasAnyRole(params UserRole[] roles)
    {
        return Role.HasValue && roles.Contains(Role.Value);
    }

    public bool HasPermission(string permission)
    {
        if (!IsAuthenticated || !Role.HasValue) return false;
        return RolePermissions.HasPermission(Role.Value, permission);
    }
}
