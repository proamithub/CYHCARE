namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Audit trail logging (via Stored Procedures).
/// </summary>
public interface IAuditRepository
{
    /// <summary>
    /// Calls sp_Audit_Insert to append an audit log entry.
    /// </summary>
    Task<long> InsertAsync(int? userId, string action, string? details);
}
