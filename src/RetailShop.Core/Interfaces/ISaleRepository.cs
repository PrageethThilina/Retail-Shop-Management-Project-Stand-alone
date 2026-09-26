using RetailShop.Core.Common;
using RetailShop.Core.Models;

namespace RetailShop.Core.Interfaces;

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(int id);
    Task<Sale?> GetByInvoiceNumberAsync(string invoiceNumber);
    Task<PagedResult<Sale>> GetPagedAsync(PagedRequest request);
    Task<string> CreateSaleAsync(Sale sale);
    Task<IReadOnlyList<SaleItem>> GetSaleItemsAsync(int saleId);
    Task<string> GenerateInvoiceNumberAsync();
}
