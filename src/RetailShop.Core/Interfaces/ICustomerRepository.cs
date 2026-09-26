using RetailShop.Core.Common;
using RetailShop.Core.Models;

namespace RetailShop.Core.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer?> GetByPhoneAsync(string phone);
    Task<PagedResult<Customer>> GetPagedAsync(PagedRequest request);
    Task<int> CreateAsync(Customer customer);
    Task<bool> UpdateAsync(Customer customer);
}
