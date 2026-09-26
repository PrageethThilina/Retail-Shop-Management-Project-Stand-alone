using Dapper;
using RetailShop.Core.Common;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;

namespace RetailShop.Data.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CustomerRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Customers WHERE Id = @Id;";
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Customer>(sql, new { Id = id });
    }

    public async Task<Customer?> GetByPhoneAsync(string phone)
    {
        const string sql = "SELECT * FROM Customers WHERE Phone = @Phone;";
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Customer>(sql, new { Phone = phone });
    }

    public async Task<PagedResult<Customer>> GetPagedAsync(PagedRequest request)
    {
        using var conn = _connectionFactory.CreateConnection();

        var searchPattern = string.IsNullOrWhiteSpace(request.Search) ? null : $"%{request.Search.Trim()}%";

        const string countSql = @"
            SELECT COUNT(*) 
            FROM Customers 
            WHERE (@Search IS NULL OR Name LIKE @Search OR Phone LIKE @Search OR Email LIKE @Search);";

        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, new { Search = searchPattern });

        const string dataSql = @"
            SELECT *
            FROM Customers
            WHERE (@Search IS NULL OR Name LIKE @Search OR Phone LIKE @Search OR Email LIKE @Search)
            ORDER BY Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await conn.QueryAsync<Customer>(dataSql, new
        {
            Search = searchPattern,
            request.Offset,
            request.PageSize
        })).ToList();

        return new PagedResult<Customer>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<int> CreateAsync(Customer customer)
    {
        const string sql = @"
            INSERT INTO Customers (Name, Phone, Email, Address, TotalPurchases, CreatedAt)
            VALUES (@Name, @Phone, @Email, @Address, @TotalPurchases, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() as int);";

        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, customer);
    }

    public async Task<bool> UpdateAsync(Customer customer)
    {
        const string sql = @"
            UPDATE Customers
            SET Name = @Name,
                Phone = @Phone,
                Email = @Email,
                Address = @Address,
                TotalPurchases = @TotalPurchases
            WHERE Id = @Id;";

        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, customer);
        return rows > 0;
    }
}
