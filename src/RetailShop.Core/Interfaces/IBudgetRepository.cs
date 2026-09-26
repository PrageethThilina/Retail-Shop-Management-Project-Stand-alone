using RetailShop.Core.Common;
using RetailShop.Core.Models;

namespace RetailShop.Core.Interfaces;

public interface IBudgetRepository
{
    Task<PagedResult<BudgetRecord>> GetPagedAsync(PagedRequest request, int? year = null);
    Task<BudgetRecord?> GetByYearMonthAsync(int year, int month);
    Task<int> SaveAsync(BudgetRecord budget);
    Task<bool> DeleteAsync(int id);
}
