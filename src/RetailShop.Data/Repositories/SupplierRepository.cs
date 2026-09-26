using Dapper;
using RetailShop.Core.Common;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;

namespace RetailShop.Data.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SupplierRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Supplier?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Suppliers WHERE Id = @Id;";
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Supplier>(sql, new { Id = id });
    }

    public async Task<PagedResult<Supplier>> GetPagedAsync(PagedRequest request)
    {
        using var conn = _connectionFactory.CreateConnection();

        var searchPattern = string.IsNullOrWhiteSpace(request.Search) ? null : $"%{request.Search.Trim()}%";

        const string countSql = @"
            SELECT COUNT(*) 
            FROM Suppliers 
            WHERE (@Search IS NULL OR SupplierCode LIKE @Search OR Name LIKE @Search OR ContactPerson LIKE @Search OR Phone LIKE @Search);";

        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, new { Search = searchPattern });

        const string dataSql = @"
            SELECT *
            FROM Suppliers
            WHERE (@Search IS NULL OR SupplierCode LIKE @Search OR Name LIKE @Search OR ContactPerson LIKE @Search OR Phone LIKE @Search)
            ORDER BY Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await conn.QueryAsync<Supplier>(dataSql, new
        {
            Search = searchPattern,
            request.Offset,
            request.PageSize
        })).ToList();

        return new PagedResult<Supplier>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<IReadOnlyList<Supplier>> GetAllActiveAsync()
    {
        const string sql = "SELECT * FROM Suppliers WHERE IsActive = 1 ORDER BY Name ASC;";
        using var conn = _connectionFactory.CreateConnection();
        var items = await conn.QueryAsync<Supplier>(sql);
        return items.ToList();
    }

    public async Task<int> CreateAsync(Supplier supplier)
    {
        const string sql = @"
            INSERT INTO Suppliers (SupplierCode, Name, ContactPerson, Phone, Email, Address, IsActive, CreatedAt)
            VALUES (@SupplierCode, @Name, @ContactPerson, @Phone, @Email, @Address, @IsActive, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() as int);";

        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, supplier);
    }

    public async Task<bool> UpdateAsync(Supplier supplier)
    {
        const string sql = @"
            UPDATE Suppliers
            SET SupplierCode = @SupplierCode,
                Name = @Name,
                ContactPerson = @ContactPerson,
                Phone = @Phone,
                Email = @Email,
                Address = @Address,
                IsActive = @IsActive
            WHERE Id = @Id;";

        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, supplier);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Suppliers WHERE Id = @Id;";
        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}
