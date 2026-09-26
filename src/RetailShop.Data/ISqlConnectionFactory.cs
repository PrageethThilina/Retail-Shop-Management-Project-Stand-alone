using Microsoft.Data.SqlClient;

namespace RetailShop.Data;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
    SqlConnection CreateMasterConnection();
    string ConnectionString { get; }
}
