using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Application.DTOs;

namespace CYHCare.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of IPaymentRepository executing Stored Procedures only.
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PaymentRepository> _logger;

    public PaymentRepository(IDbConnectionFactory connectionFactory, ILogger<PaymentRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<HelperEarningsDto>> GetByHelperAsync(int helperId)
    {
        _logger.LogInformation("Calling sp_Payment_GetByHelper for HelperId: {HelperId}", helperId);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@HelperId", helperId);

        return await connection.QueryAsync<HelperEarningsDto>(
            "sp_Payment_GetByHelper",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<PaymentDto?> MarkPaidAsync(int paymentId, int? adminUserId = null)
    {
        _logger.LogInformation("Calling sp_Payment_MarkPaid for PaymentId: {PaymentId}", paymentId);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@PaymentId", paymentId);
        parameters.Add("@AdminUserId", adminUserId);

        return await connection.QuerySingleOrDefaultAsync<PaymentDto>(
            "sp_Payment_MarkPaid",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
