using RetailShop.Core.Common;
using RetailShop.Core.Models;

namespace RetailShop.Core.Interfaces;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(int id);
    Task<PagedResult<Supplier>> GetPagedAsync(PagedRequest request);
    Task<IReadOnlyList<Supplier>> GetAllActiveAsync();
    Task<int> CreateAsync(Supplier supplier);
    Task<bool> UpdateAsync(Supplier supplier);
    Task<bool> DeleteAsync(int id);
}
