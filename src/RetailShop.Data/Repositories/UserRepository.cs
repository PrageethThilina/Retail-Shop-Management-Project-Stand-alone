using Dapper;
using RetailShop.Core.Common;
using RetailShop.Core.Interfaces;
using RetailShop.Core.Models;
using RetailShop.Core.Security;

namespace RetailShop.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IPasswordHasher _passwordHasher;

    public UserRepository(ISqlConnectionFactory connectionFactory, IPasswordHasher passwordHasher)
    {
        _connectionFactory = connectionFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        const string sql = "SELECT * FROM Users WHERE Id = @Id";
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = "SELECT * FROM Users WHERE Username = @Username";
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<User?> AuthenticateAsync(string username, string plainPassword)
    {
        const string sql = "SELECT * FROM Users WHERE Username = @Username AND IsActive = 1";
        using var conn = _connectionFactory.CreateConnection();
        var user = await conn.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });

        if (user == null)
            return null;

        if (!_passwordHasher.VerifyPassword(plainPassword, user.PasswordHash))
            return null;

        await conn.ExecuteAsync("UPDATE Users SET LastLoginAt = GETUTCDATE() WHERE Id = @Id", new { user.Id });
        user.LastLoginAt = DateTime.UtcNow;

        return user;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        using var conn = _connectionFactory.CreateConnection();
        var user = await conn.QuerySingleOrDefaultAsync<User>("SELECT * FROM Users WHERE Id = @Id", new { Id = userId });

        if (user == null || !_passwordHasher.VerifyPassword(oldPassword, user.PasswordHash))
            return false;

        var newHash = _passwordHasher.HashPassword(newPassword);
        var rows = await conn.ExecuteAsync("UPDATE Users SET PasswordHash = @Hash WHERE Id = @Id", new { Hash = newHash, Id = userId });
        return rows > 0;
    }

    public async Task<PagedResult<User>> GetPagedAsync(PagedRequest request)
    {
        using var conn = _connectionFactory.CreateConnection();

        var searchPattern = string.IsNullOrWhiteSpace(request.Search) ? null : $"%{request.Search.Trim()}%";

        const string countSql = @"
            SELECT COUNT(*) 
            FROM Users 
            WHERE (@Search IS NULL OR Username LIKE @Search OR FullName LIKE @Search OR Email LIKE @Search);";

        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, new { Search = searchPattern });

        const string dataSql = @"
            SELECT Id, Username, FullName, Email, Address, DateOfBirth, Role, IsActive, CreatedAt, LastLoginAt
            FROM Users
            WHERE (@Search IS NULL OR Username LIKE @Search OR FullName LIKE @Search OR Email LIKE @Search)
            ORDER BY Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var items = (await conn.QueryAsync<User>(dataSql, new
        {
            Search = searchPattern,
            request.Offset,
            request.PageSize
        })).ToList();

        return new PagedResult<User>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<int> CreateAsync(User user, string plainPassword)
    {
        user.PasswordHash = _passwordHasher.HashPassword(plainPassword);

        const string sql = @"
            INSERT INTO Users (Username, PasswordHash, FullName, Email, Address, DateOfBirth, Role, IsActive, CreatedAt)
            VALUES (@Username, @PasswordHash, @FullName, @Email, @Address, @DateOfBirth, @Role, @IsActive, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() as int);";

        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, user);
    }

    public async Task<bool> UpdateAsync(User user)
    {
        const string sql = @"
            UPDATE Users
            SET FullName = @FullName,
                Email = @Email,
                Address = @Address,
                DateOfBirth = @DateOfBirth,
                Role = @Role,
                IsActive = @IsActive
            WHERE Id = @Id;";

        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, user);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM Users WHERE Id = @Id;";
        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<bool> UsernameExistsAsync(string username, int? excludeId = null)
    {
        const string sql = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND (@ExcludeId IS NULL OR Id <> @ExcludeId);";
        using var conn = _connectionFactory.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(sql, new { Username = username, ExcludeId = excludeId });
        return count > 0;
    }
}
