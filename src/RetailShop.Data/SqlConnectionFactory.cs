using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace RetailShop.Data;

public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;
    private readonly string _masterConnectionString;

    public string ConnectionString => _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        var configString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(configString))
        {
            _connectionString = @"Server=.\SQLEXPRESS;Database=RetailShopDb;Integrated Security=True;TrustServerCertificate=True;";
        }
        else
        {
            _connectionString = configString;
        }

        var builder = new SqlConnectionStringBuilder(_connectionString)
        {
            InitialCatalog = "master"
        };
        _masterConnectionString = builder.ConnectionString;
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public SqlConnection CreateMasterConnection()
    {
        return new SqlConnection(_masterConnectionString);
    }
}
