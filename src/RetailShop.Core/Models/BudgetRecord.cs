namespace RetailShop.Core.Models;

public class BudgetRecord
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal InventoryCost { get; set; }
    public decimal WaterBill { get; set; }
    public decimal ElectricityBill { get; set; }
    public decimal SalariesTotal { get; set; }
    public decimal OtherExpenses { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses => InventoryCost + WaterBill + ElectricityBill + SalariesTotal + OtherExpenses;
    public decimal NetIncomeMonth => TotalIncome - TotalExpenses;
    public decimal NetIncomeYear { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
