using RetailShop.Core.Enums;

namespace RetailShop.Core.Security;

public static class RolePermissions
{
    public const string ManageUsers = "ManageUsers";
    public const string ManageEmployees = "ManageEmployees";
    public const string ManageSalaries = "ManageSalaries";
    public const string ManageBudget = "ManageBudget";
    public const string ManageProducts = "ManageProducts";
    public const string ManageSuppliers = "ManageSuppliers";
    public const string PerformBilling = "PerformBilling";
    public const string ViewCustomers = "ViewCustomers";
    public const string ViewDashboard = "ViewDashboard";
    public const string ViewReports = "ViewReports";

    private static readonly Dictionary<UserRole, HashSet<string>> _rolePermissions = new()
    {
        [UserRole.Admin] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ManageUsers,
            ManageEmployees,
            ManageSalaries,
            ManageBudget,
            ManageProducts,
            ManageSuppliers,
            PerformBilling,
            ViewCustomers,
            ViewDashboard,
            ViewReports
        },
        [UserRole.Manager] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ManageEmployees,
            ManageProducts,
            ManageSuppliers,
            PerformBilling,
            ViewCustomers,
            ViewDashboard,
            ViewReports
        },
        [UserRole.Cashier] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PerformBilling,
            ViewCustomers,
            ViewDashboard
        }
    };

    public static bool HasPermission(UserRole role, string permission)
    {
        return _rolePermissions.TryGetValue(role, out var permissions) && permissions.Contains(permission);
    }

    public static IReadOnlyCollection<string> GetPermissions(UserRole role)
    {
        return _rolePermissions.TryGetValue(role, out var permissions) 
            ? permissions 
            : Array.Empty<string>();
    }
}
