namespace RetailShop.Core.Models;

public class DashboardMetrics
{
    public decimal TodaySales { get; set; }
    public int TodayInvoicesCount { get; set; }
    public decimal MonthSales { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public int TotalSuppliers { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalCustomers { get; set; }
}

public class DailySalesPoint
{
    public DateTime Date { get; set; }
    public string DayLabel { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int OrderCount { get; set; }
}

public class CategoryDistributionItem
{
    public string Category { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal TotalStockValue { get; set; }
    public double Percentage { get; set; }
}
