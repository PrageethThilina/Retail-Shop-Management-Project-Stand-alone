using RetailShop.Core.Common;
using RetailShop.Core.Models;

namespace RetailShop.Core.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetByCodeAsync(string productCode);
    Task<PagedResult<Product>> GetPagedAsync(PagedRequest request);
    Task<IReadOnlyList<Product>> GetAllActiveAsync();
    Task<IReadOnlyList<Product>> GetLowStockProductsAsync(int threshold = 5);
    Task<int> CreateAsync(Product product);
    Task<bool> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeductStockAsync(int productId, int quantity);
    Task<bool> ProductCodeExistsAsync(string code, int? excludeId = null);
}
