using Dapper;
using RetailShop.Core.Common;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;

namespace RetailShop.Data.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public BudgetRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<BudgetRecord>> GetPagedAsync(PagedRequest request, int? year = null)
    {
        using var conn = _connectionFactory.CreateConnection();

        var searchPattern = string.IsNullOrWhiteSpace(request.Search) ? null : $"%{request.Search.Trim()}%";

        const string countSql = @"
            SELECT COUNT(*) 
            FROM Budgets 
            WHERE (@Year IS NULL OR Year = @Year)
              AND (@Search IS NULL OR Notes LIKE @Search);";

        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, new
        {
            Search = searchPattern,
            Year = year
        });

        const string dataSql = @"
            SELECT *
            FROM Budgets
            WHERE (@Year IS NULL OR Year = @Year)
              AND (@Search IS NULL OR Notes LIKE @Search)
            ORDER BY Year DESC, Month DESC, Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await conn.QueryAsync<BudgetRecord>(dataSql, new
        {
            Search = searchPattern,
            Year = year,
            request.Offset,
            request.PageSize
        })).ToList();

        return new PagedResult<BudgetRecord>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<BudgetRecord?> GetByYearMonthAsync(int year, int month)
    {
        const string sql = "SELECT * FROM Budgets WHERE Year = @Year AND Month = @Month;";
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BudgetRecord>(sql, new { Year = year, Month = month });
    }

    public async Task<int> SaveAsync(BudgetRecord budget)
    {
        const string sql = @"
            IF EXISTS (SELECT 1 FROM Budgets WHERE Year = @Year AND Month = @Month)
            BEGIN
                UPDATE Budgets
                SET InventoryCost = @InventoryCost,
                    WaterBill = @WaterBill,
                    ElectricityBill = @ElectricityBill,
                    SalariesTotal = @SalariesTotal,
                    OtherExpenses = @OtherExpenses,
                    TotalIncome = @TotalIncome,
                    NetIncomeYear = @NetIncomeYear,
                    Notes = @Notes,
                    RecordedAt = GETUTCDATE()
                WHERE Year = @Year AND Month = @Month;
                SELECT Id FROM Budgets WHERE Year = @Year AND Month = @Month;
            END
            ELSE
            BEGIN
                INSERT INTO Budgets (Year, Month, InventoryCost, WaterBill, ElectricityBill, SalariesTotal, OtherExpenses, TotalIncome, NetIncomeYear, Notes, RecordedAt)
                VALUES (@Year, @Month, @InventoryCost, @WaterBill, @ElectricityBill, @SalariesTotal, @OtherExpenses, @TotalIncome, @NetIncomeYear, @Notes, GETUTCDATE());
                SELECT CAST(SCOPE_IDENTITY() as int);
            END";

        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, budget);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Budgets WHERE Id = @Id;";
        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }
}
