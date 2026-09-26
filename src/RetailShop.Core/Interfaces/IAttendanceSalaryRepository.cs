using RetailShop.Core.Common;
using RetailShop.Core.Models;

namespace RetailShop.Core.Interfaces;

public interface IAttendanceSalaryRepository
{
    Task<PagedResult<Attendance>> GetAttendancePagedAsync(PagedRequest request, int? year = null, int? month = null);
    Task<int> SaveAttendanceAsync(Attendance attendance);
    Task<PagedResult<SalaryRecord>> GetSalaryPagedAsync(PagedRequest request, int? year = null, int? month = null);
    Task<int> SaveSalaryAsync(SalaryRecord salary);
    Task<decimal> GetTotalSalaryExpenseAsync(int year, int month);
}
