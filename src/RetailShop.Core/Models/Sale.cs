using RetailShop.Core.Enums;

namespace RetailShop.Core.Models;

public class Sale
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = "Walk-in Customer";
    public string CustomerPhone { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal NetTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public int CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public List<SaleItem> Items { get; set; } = new();
}
