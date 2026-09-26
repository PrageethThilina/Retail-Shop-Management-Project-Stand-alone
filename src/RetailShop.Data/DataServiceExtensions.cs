using Microsoft.Extensions.DependencyInjection;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Security;
using RetailShop.Data.Repositories;

namespace RetailShop.Data;

public static class DataServiceExtensions
{
    public static IServiceCollection AddRetailShopData(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddSingleton<IDbInitializer, DbInitializer>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAttendanceSalaryRepository, AttendanceSalaryRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        return services;
    }
}
