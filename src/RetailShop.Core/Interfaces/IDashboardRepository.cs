using RetailShop.Core.Models;

namespace RetailShop.Core.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardMetrics> GetMetricsAsync();
    Task<List<DailySalesPoint>> GetWeeklySalesTrendAsync();
    Task<List<CategoryDistributionItem>> GetCategoryDistributionAsync();
}
