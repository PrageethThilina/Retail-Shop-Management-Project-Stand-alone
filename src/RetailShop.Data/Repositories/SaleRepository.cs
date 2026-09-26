using Dapper;
using RetailShop.Core.Common;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;

namespace RetailShop.Data.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SaleRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Sale?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Sales WHERE Id = @Id;";
        using var conn = _connectionFactory.CreateConnection();
        var sale = await conn.QuerySingleOrDefaultAsync<Sale>(sql, new { Id = id });

        if (sale != null)
        {
            sale.Items = (await GetSaleItemsAsync(sale.Id)).ToList();
        }

        return sale;
    }

    public async Task<Sale?> GetByInvoiceNumberAsync(string invoiceNumber)
    {
        const string sql = "SELECT * FROM Sales WHERE InvoiceNumber = @InvoiceNumber;";
        using var conn = _connectionFactory.CreateConnection();
        var sale = await conn.QuerySingleOrDefaultAsync<Sale>(sql, new { InvoiceNumber = invoiceNumber });

        if (sale != null)
        {
            sale.Items = (await GetSaleItemsAsync(sale.Id)).ToList();
        }

        return sale;
    }

    public async Task<PagedResult<Sale>> GetPagedAsync(PagedRequest request)
    {
        using var conn = _connectionFactory.CreateConnection();

        var searchPattern = string.IsNullOrWhiteSpace(request.Search) ? null : $"%{request.Search.Trim()}%";

        const string countSql = @"
            SELECT COUNT(*) 
            FROM Sales 
            WHERE (@Search IS NULL OR InvoiceNumber LIKE @Search OR CustomerName LIKE @Search OR CashierName LIKE @Search);";

        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, new { Search = searchPattern });

        const string dataSql = @"
            SELECT *
            FROM Sales
            WHERE (@Search IS NULL OR InvoiceNumber LIKE @Search OR CustomerName LIKE @Search OR CashierName LIKE @Search)
            ORDER BY SaleDate DESC, Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await conn.QueryAsync<Sale>(dataSql, new
        {
            Search = searchPattern,
            request.Offset,
            request.PageSize
        })).ToList();

        return new PagedResult<Sale>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<IReadOnlyList<SaleItem>> GetSaleItemsAsync(int saleId)
    {
        const string sql = "SELECT * FROM SaleItems WHERE SaleId = @SaleId;";
        using var conn = _connectionFactory.CreateConnection();
        var items = await conn.QueryAsync<SaleItem>(sql, new { SaleId = saleId });
        return items.ToList();
    }

    public async Task<string> GenerateInvoiceNumberAsync()
    {
        var datePrefix = $"INV-{DateTime.UtcNow:yyyyMMdd}-";
        const string sql = "SELECT COUNT(*) FROM Sales WHERE InvoiceNumber LIKE @Prefix + '%';";

        using var conn = _connectionFactory.CreateConnection();
        var countToday = await conn.ExecuteScalarAsync<int>(sql, new { Prefix = datePrefix });
        return $"{datePrefix}{(countToday + 1):D4}";
    }

    public async Task<string> CreateSaleAsync(Sale sale)
    {
        if (sale.Items == null || sale.Items.Count == 0)
            throw new InvalidOperationException("Cannot checkout an empty sale.");

        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            if (string.IsNullOrWhiteSpace(sale.InvoiceNumber))
            {
                var datePrefix = $"INV-{DateTime.UtcNow:yyyyMMdd}-";
                const string countSql = "SELECT COUNT(*) FROM Sales WITH (UPDLOCK, HOLDLOCK) WHERE InvoiceNumber LIKE @Prefix + '%';";
                var countToday = await conn.ExecuteScalarAsync<int>(countSql, new { Prefix = datePrefix }, transaction);
                sale.InvoiceNumber = $"{datePrefix}{(countToday + 1):D4}";
            }

            const string insertSaleSql = @"
                INSERT INTO Sales (InvoiceNumber, CustomerName, CustomerPhone, SubTotal, Discount, NetTotal, PaidAmount, Balance, PaymentMethod, CashierId, CashierName, SaleDate)
                VALUES (@InvoiceNumber, @CustomerName, @CustomerPhone, @SubTotal, @Discount, @NetTotal, @PaidAmount, @Balance, @PaymentMethod, @CashierId, @CashierName, GETUTCDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var saleId = await conn.ExecuteScalarAsync<int>(insertSaleSql, sale, transaction);
            sale.Id = saleId;

            const string insertItemSql = @"
                INSERT INTO SaleItems (SaleId, ProductId, ProductCode, ProductName, UnitPrice, Quantity, TotalPrice)
                VALUES (@SaleId, @ProductId, @ProductCode, @ProductName, @UnitPrice, @Quantity, @TotalPrice);";

            const string deductStockSql = @"
                UPDATE Products
                SET Quantity = Quantity - @Quantity,
                    UpdatedAt = GETUTCDATE()
                WHERE Id = @ProductId AND Quantity >= @Quantity;";

            foreach (var item in sale.Items)
            {
                item.SaleId = saleId;

                // Atomic stock deduction with inventory check
                var affected = await conn.ExecuteAsync(deductStockSql, new
                {
                    Quantity = item.Quantity,
                    ProductId = item.ProductId
                }, transaction);

                if (affected == 0)
                {
                    throw new InvalidOperationException($"Insufficient inventory available for item: {item.ProductName} ({item.ProductCode}).");
                }

                await conn.ExecuteAsync(insertItemSql, item, transaction);
            }

            // If Customer phone is provided, update or insert customer loyalty/purchases
            if (!string.IsNullOrWhiteSpace(sale.CustomerPhone))
            {
                const string updateCustSql = @"
                    IF EXISTS (SELECT 1 FROM Customers WHERE Phone = @Phone)
                    BEGIN
                        UPDATE Customers 
                        SET TotalPurchases = TotalPurchases + @NetTotal,
                            Name = CASE WHEN @Name <> '' AND @Name <> 'Walk-in Customer' THEN @Name ELSE Name END
                        WHERE Phone = @Phone;
                    END
                    ELSE IF @Name <> 'Walk-in Customer' AND @Name <> ''
                    BEGIN
                        INSERT INTO Customers (Name, Phone, TotalPurchases, CreatedAt)
                        VALUES (@Name, @Phone, @NetTotal, GETUTCDATE());
                    END";

                await conn.ExecuteAsync(updateCustSql, new
                {
                    Phone = sale.CustomerPhone,
                    Name = sale.CustomerName,
                    NetTotal = sale.NetTotal
                }, transaction);
            }

            transaction.Commit();
            return sale.InvoiceNumber;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
