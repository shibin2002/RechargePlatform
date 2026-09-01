using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace RechargePlatform.Data.Database;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
    SqlConnection CreateSqlConnection();
    string ConnectionString { get; }
}

public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Database connection string not configured. Set DB_CONNECTION_STRING environment variable or provide DefaultConnection in configuration.");
    }

    public string ConnectionString => _connectionString;

    public IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(_connectionString);
        return connection;
    }

    public SqlConnection CreateSqlConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
