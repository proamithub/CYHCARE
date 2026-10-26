using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CYHCare.Application.Common.Interfaces;

namespace CYHCare.Infrastructure.Data;

/// <summary>
/// SQL Server implementation of IDbConnectionFactory using Microsoft.Data.SqlClient.
/// </summary>
public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured in appsettings.json.");
    }

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
