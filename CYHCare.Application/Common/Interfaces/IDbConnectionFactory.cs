using System.Data;

namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Factory interface for creating database connections for Dapper execution.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates and returns a new database connection.
    /// </summary>
    IDbConnection CreateConnection();

    /// <summary>
    /// Creates and opens a new database connection asynchronously.
    /// </summary>
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
