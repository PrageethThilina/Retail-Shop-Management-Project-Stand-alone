using Dapper;
using RetailShop.Core.Common;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;

namespace RetailShop.Data.Repositories;

public class AttendanceSalaryRepository : IAttendanceSalaryRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AttendanceSalaryRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<Attendance>> GetAttendancePagedAsync(PagedRequest request, int? year = null, int? month = null)
    {
        using var conn = _connectionFactory.CreateConnection();

        var searchPattern = string.IsNullOrWhiteSpace(request.Search) ? null : $"%{request.Search.Trim()}%";

        const string countSql = @"
            SELECT COUNT(*) 
            FROM Attendances 
            WHERE (@Search IS NULL OR EmployeeName LIKE @Search)
              AND (@Year IS NULL OR Year = @Year)
              AND (@Month IS NULL OR Month = @Month);";

        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, new
        {
            Search = searchPattern,
            Year = year,
            Month = month
        });

        const string dataSql = @"
            SELECT *
            FROM Attendances
            WHERE (@Search IS NULL OR EmployeeName LIKE @Search)
              AND (@Year IS NULL OR Year = @Year)
              AND (@Month IS NULL OR Month = @Month)
            ORDER BY Year DESC, Month DESC, Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await conn.QueryAsync<Attendance>(dataSql, new
        {
            Search = searchPattern,
            Year = year,
            Month = month,
            request.Offset,
            request.PageSize
        })).ToList();

        return new PagedResult<Attendance>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<int> SaveAttendanceAsync(Attendance attendance)
    {
        const string sql = @"
            IF EXISTS (SELECT 1 FROM Attendances WHERE EmployeeId = @EmployeeId AND Year = @Year AND Month = @Month)
            BEGIN
                UPDATE Attendances
                SET EmployeeName = @EmployeeName,
                    TotalDays = @TotalDays,
                    WorkingDays = @WorkingDays,
                    PresentDays = @PresentDays,
                    AbsentDays = @AbsentDays,
                    RecordedAt = GETUTCDATE()
                WHERE EmployeeId = @EmployeeId AND Year = @Year AND Month = @Month;
                SELECT Id FROM Attendances WHERE EmployeeId = @EmployeeId AND Year = @Year AND Month = @Month;
            END
            ELSE
            BEGIN
                INSERT INTO Attendances (EmployeeId, EmployeeName, Year, Month, TotalDays, WorkingDays, PresentDays, AbsentDays, RecordedAt)
                VALUES (@EmployeeId, @EmployeeName, @Year, @Month, @TotalDays, @WorkingDays, @PresentDays, @AbsentDays, GETUTCDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);
            END";

        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, attendance);
    }

    public async Task<PagedResult<SalaryRecord>> GetSalaryPagedAsync(PagedRequest request, int? year = null, int? month = null)
    {
        using var conn = _connectionFactory.CreateConnection();

        var searchPattern = string.IsNullOrWhiteSpace(request.Search) ? null : $"%{request.Search.Trim()}%";

        const string countSql = @"
            SELECT COUNT(*) 
            FROM Salaries 
            WHERE (@Search IS NULL OR EmployeeName LIKE @Search)
              AND (@Year IS NULL OR Year = @Year)
              AND (@Month IS NULL OR Month = @Month);";

        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, new
        {
            Search = searchPattern,
            Year = year,
            Month = month
        });

        const string dataSql = @"
            SELECT *
            FROM Salaries
            WHERE (@Search IS NULL OR EmployeeName LIKE @Search)
              AND (@Year IS NULL OR Year = @Year)
              AND (@Month IS NULL OR Month = @Month)
            ORDER BY Year DESC, Month DESC, Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await conn.QueryAsync<SalaryRecord>(dataSql, new
        {
            Search = searchPattern,
            Year = year,
            Month = month,
            request.Offset,
            request.PageSize
        })).ToList();

        return new PagedResult<SalaryRecord>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<int> SaveSalaryAsync(SalaryRecord salary)
    {
        const string sql = @"
            IF EXISTS (SELECT 1 FROM Salaries WHERE EmployeeId = @EmployeeId AND Year = @Year AND Month = @Month)
            BEGIN
                UPDATE Salaries
                SET EmployeeName = @EmployeeName,
                    BasicSalary = @BasicSalary,
                    DailyRate = @DailyRate,
                    Allowances = @Allowances,
                    Deductions = @Deductions,
                    NetSalary = @NetSalary,
                    PaidDate = GETUTCDATE(),
                    Notes = @Notes
                WHERE EmployeeId = @EmployeeId AND Year = @Year AND Month = @Month;
                SELECT Id FROM Salaries WHERE EmployeeId = @EmployeeId AND Year = @Year AND Month = @Month;
            END
            ELSE
            BEGIN
                INSERT INTO Salaries (EmployeeId, EmployeeName, Year, Month, BasicSalary, DailyRate, Allowances, Deductions, NetSalary, PaidDate, Notes)
                VALUES (@EmployeeId, @EmployeeName, @Year, @Month, @BasicSalary, @DailyRate, @Allowances, @Deductions, @NetSalary, GETUTCDATE(), @Notes);
                SELECT CAST(SCOPE_IDENTITY() as int);
            END";

        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, salary);
    }

    public async Task<decimal> GetTotalSalaryExpenseAsync(int year, int month)
    {
        const string sql = "SELECT ISNULL(SUM(NetSalary), 0) FROM Salaries WHERE Year = @Year AND Month = @Month;";
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<decimal>(sql, new { Year = year, Month = month });
    }
}
