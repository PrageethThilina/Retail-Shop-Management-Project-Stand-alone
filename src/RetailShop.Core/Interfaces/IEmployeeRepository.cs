using RetailShop.Core.Common;
using RetailShop.Core.Models;

namespace RetailShop.Core.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int id);
    Task<PagedResult<Employee>> GetPagedAsync(PagedRequest request);
    Task<IReadOnlyList<Employee>> GetAllActiveAsync();
    Task<int> CreateAsync(Employee employee);
    Task<bool> UpdateAsync(Employee employee);
    Task<bool> DeleteAsync(int id);
}
