using Dapper;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;

namespace RetailShop.Data.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public DashboardRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DashboardMetrics> GetMetricsAsync()
    {
        using var conn = _connectionFactory.CreateConnection();

        const string sql = @"
            -- Today's sales & count
            SELECT 
                ISNULL(SUM(NetTotal), 0) AS TodaySales,
                COUNT(*) AS TodayInvoicesCount
            FROM Sales
            WHERE CAST(SaleDate AS DATE) = CAST(GETUTCDATE() AS DATE);

            -- Current Month sales
            SELECT ISNULL(SUM(NetTotal), 0)
            FROM Sales
            WHERE YEAR(SaleDate) = YEAR(GETUTCDATE()) AND MONTH(SaleDate) = MONTH(GETUTCDATE());

            -- Products total
            SELECT COUNT(*) FROM Products WHERE IsActive = 1;

            -- Low stock products count
            SELECT COUNT(*) FROM Products WHERE IsActive = 1 AND Quantity <= LowStockThreshold;

            -- Total suppliers
            SELECT COUNT(*) FROM Suppliers WHERE IsActive = 1;

            -- Total employees
            SELECT COUNT(*) FROM Employees WHERE IsActive = 1;

            -- Total customers
            SELECT COUNT(*) FROM Customers;
        ";

        using var multi = await conn.QueryMultipleAsync(sql);

        var todayData = await multi.ReadSingleAsync();
        var monthSales = await multi.ReadSingleAsync<decimal>();
        var totalProducts = await multi.ReadSingleAsync<int>();
        var lowStock = await multi.ReadSingleAsync<int>();
        var totalSuppliers = await multi.ReadSingleAsync<int>();
        var totalEmployees = await multi.ReadSingleAsync<int>();
        var totalCustomers = await multi.ReadSingleAsync<int>();

        return new DashboardMetrics
        {
            TodaySales = (decimal)todayData.TodaySales,
            TodayInvoicesCount = (int)todayData.TodayInvoicesCount,
            MonthSales = monthSales,
            TotalProducts = totalProducts,
            LowStockCount = lowStock,
            TotalSuppliers = totalSuppliers,
            TotalEmployees = totalEmployees,
            TotalCustomers = totalCustomers
        };
    }

    public async Task<List<DailySalesPoint>> GetWeeklySalesTrendAsync()
    {
        using var conn = _connectionFactory.CreateConnection();

        const string sql = @"
            SELECT 
                CAST(SaleDate AS DATE) AS [Date],
                ISNULL(SUM(NetTotal), 0) AS Amount,
                COUNT(*) AS OrderCount
            FROM Sales
            WHERE SaleDate >= DATEADD(DAY, -6, CAST(GETUTCDATE() AS DATE))
            GROUP BY CAST(SaleDate AS DATE)
            ORDER BY [Date] ASC;
        ";

        var rows = (await conn.QueryAsync(sql)).ToList();
        var map = new Dictionary<DateTime, (decimal Amount, int Count)>();
        foreach (var r in rows)
        {
            DateTime d = (DateTime)r.Date;
            map[d.Date] = ((decimal)r.Amount, (int)r.OrderCount);
        }

        var result = new List<DailySalesPoint>();
        var today = DateTime.UtcNow.Date;

        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            if (map.TryGetValue(date, out var val))
            {
                result.Add(new DailySalesPoint
                {
                    Date = date,
                    DayLabel = date.ToString("ddd d"),
                    Amount = val.Amount,
                    OrderCount = val.Count
                });
            }
            else
            {
                result.Add(new DailySalesPoint
                {
                    Date = date,
                    DayLabel = date.ToString("ddd d"),
                    Amount = 0m,
                    OrderCount = 0
                });
            }
        }

        return result;
    }

    public async Task<List<CategoryDistributionItem>> GetCategoryDistributionAsync()
    {
        using var conn = _connectionFactory.CreateConnection();

        const string sql = @"
            SELECT 
                ISNULL(Category, 'Uncategorized') AS Category,
                COUNT(*) AS ProductCount,
                ISNULL(SUM(Quantity * SellingPrice), 0) AS TotalStockValue
            FROM Products
            WHERE IsActive = 1
            GROUP BY Category
            ORDER BY TotalStockValue DESC;
        ";

        var list = (await conn.QueryAsync(sql))
            .Select(r => new CategoryDistributionItem
            {
                Category = (string)r.Category,
                ProductCount = (int)r.ProductCount,
                TotalStockValue = (decimal)r.TotalStockValue
            })
            .ToList();

        var totalVal = list.Sum(x => x.TotalStockValue);
        if (totalVal > 0)
        {
            foreach (var item in list)
            {
                item.Percentage = Math.Round((double)(item.TotalStockValue / totalVal * 100), 1);
            }
        }

        return list;
    }
}
