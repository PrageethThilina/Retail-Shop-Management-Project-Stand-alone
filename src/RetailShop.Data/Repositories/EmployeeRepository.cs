using Dapper;
using RetailShop.Core.Common;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;

namespace RetailShop.Data.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public EmployeeRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Employees WHERE Id = @Id;";
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Employee>(sql, new { Id = id });
    }

    public async Task<PagedResult<Employee>> GetPagedAsync(PagedRequest request)
    {
        using var conn = _connectionFactory.CreateConnection();

        var searchPattern = string.IsNullOrWhiteSpace(request.Search) ? null : $"%{request.Search.Trim()}%";

        const string countSql = @"
            SELECT COUNT(*) 
            FROM Employees 
            WHERE (@Search IS NULL OR EmployeeCode LIKE @Search OR Name LIKE @Search OR Nic LIKE @Search OR Phone LIKE @Search);";

        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, new { Search = searchPattern });

        const string dataSql = @"
            SELECT *
            FROM Employees
            WHERE (@Search IS NULL OR EmployeeCode LIKE @Search OR Name LIKE @Search OR Nic LIKE @Search OR Phone LIKE @Search)
            ORDER BY Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await conn.QueryAsync<Employee>(dataSql, new
        {
            Search = searchPattern,
            request.Offset,
            request.PageSize
        })).ToList();

        return new PagedResult<Employee>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<IReadOnlyList<Employee>> GetAllActiveAsync()
    {
        const string sql = "SELECT * FROM Employees WHERE IsActive = 1 ORDER BY Name ASC;";
        using var conn = _connectionFactory.CreateConnection();
        var items = await conn.QueryAsync<Employee>(sql);
        return items.ToList();
    }

    public async Task<int> CreateAsync(Employee employee)
    {
        const string sql = @"
            INSERT INTO Employees (EmployeeCode, Name, Nic, Phone, Email, Address, DateOfBirth, Designation, BasicSalary, BankDetails, JoinDate, IsActive)
            VALUES (@EmployeeCode, @Name, @Nic, @Phone, @Email, @Address, @DateOfBirth, @Designation, @BasicSalary, @BankDetails, @JoinDate, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() as int);";

        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, employee);
    }

    public async Task<bool> UpdateAsync(Employee employee)
    {
        const string sql = @"
            UPDATE Employees
            SET EmployeeCode = @EmployeeCode,
                Name = @Name,
                Nic = @Nic,
                Phone = @Phone,
                Email = @Email,
                Address = @Address,
                DateOfBirth = @DateOfBirth,
                Designation = @Designation,
                BasicSalary = @BasicSalary,
                BankDetails = @BankDetails,
                IsActive = @IsActive
            WHERE Id = @Id;";

        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, employee);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Employees WHERE Id = @Id;";
        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}
