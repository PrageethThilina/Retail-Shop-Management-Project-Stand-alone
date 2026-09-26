namespace RetailShop.Core.Models;

public class SalaryRecord
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal DailyRate { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetSalary { get; set; }
    public DateTime PaidDate { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = string.Empty;
}
