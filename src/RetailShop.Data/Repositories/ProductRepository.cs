using Dapper;
using RetailShop.Core.Common;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;

namespace RetailShop.Data.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ProductRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Products WHERE Id = @Id;";
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id });
    }

    public async Task<Product?> GetByCodeAsync(string productCode)
    {
        const string sql = "SELECT * FROM Products WHERE ProductCode = @ProductCode;";
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Product>(sql, new { ProductCode = productCode });
    }

    public async Task<PagedResult<Product>> GetPagedAsync(PagedRequest request)
    {
        using var conn = _connectionFactory.CreateConnection();

        var searchPattern = string.IsNullOrWhiteSpace(request.Search) ? null : $"%{request.Search.Trim()}%";

        const string countSql = @"
            SELECT COUNT(*) 
            FROM Products 
            WHERE (@Search IS NULL OR ProductCode LIKE @Search OR Name LIKE @Search OR Category LIKE @Search);";

        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, new { Search = searchPattern });

        const string dataSql = @"
            SELECT *
            FROM Products
            WHERE (@Search IS NULL OR ProductCode LIKE @Search OR Name LIKE @Search OR Category LIKE @Search)
            ORDER BY Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await conn.QueryAsync<Product>(dataSql, new
        {
            Search = searchPattern,
            request.Offset,
            request.PageSize
        })).ToList();

        return new PagedResult<Product>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<IReadOnlyList<Product>> GetAllActiveAsync()
    {
        const string sql = "SELECT * FROM Products WHERE IsActive = 1 ORDER BY Name ASC;";
        using var conn = _connectionFactory.CreateConnection();
        var items = await conn.QueryAsync<Product>(sql);
        return items.ToList();
    }

    public async Task<IReadOnlyList<Product>> GetLowStockProductsAsync(int threshold = 5)
    {
        const string sql = "SELECT * FROM Products WHERE IsActive = 1 AND Quantity <= LowStockThreshold ORDER BY Quantity ASC;";
        using var conn = _connectionFactory.CreateConnection();
        var items = await conn.QueryAsync<Product>(sql);
        return items.ToList();
    }

    public async Task<int> CreateAsync(Product product)
    {
        const string sql = @"
            INSERT INTO Products (ProductCode, Name, Category, BuyingPrice, SellingPrice, Quantity, LowStockThreshold, IsActive, CreatedAt)
            VALUES (@ProductCode, @Name, @Category, @BuyingPrice, @SellingPrice, @Quantity, @LowStockThreshold, @IsActive, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() as int);";

        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, product);
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        const string sql = @"
            UPDATE Products
            SET ProductCode = @ProductCode,
                Name = @Name,
                Category = @Category,
                BuyingPrice = @BuyingPrice,
                SellingPrice = @SellingPrice,
                Quantity = @Quantity,
                LowStockThreshold = @LowStockThreshold,
                IsActive = @IsActive,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id;";

        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, product);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Products WHERE Id = @Id;";
        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<bool> DeductStockAsync(int productId, int quantity)
    {
        const string sql = @"
            UPDATE Products
            SET Quantity = Quantity - @Quantity,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND Quantity >= @Quantity;";

        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { Id = productId, Quantity = quantity });
        return rows > 0;
    }

    public async Task<bool> ProductCodeExistsAsync(string code, int? excludeId = null)
    {
        const string sql = "SELECT COUNT(*) FROM Products WHERE ProductCode = @Code AND (@ExcludeId IS NULL OR Id <> @ExcludeId);";
        using var conn = _connectionFactory.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(sql, new { Code = code, ExcludeId = excludeId });
        return count > 0;
    }
}
