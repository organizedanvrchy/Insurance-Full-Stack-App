using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Insurance.BuildingBlocks.Data;

public interface IDbConnectionFactory
{
    SqlConnection Create();
}

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration config)
    {
        // Every service has its own database, always under this name in its own configuration.
        _connectionString = config.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is missing.");
    }

    // A new connection per operation. ADO.NET pools them under the hood, so this is cheap.
    public SqlConnection Create() => new(_connectionString);
}
