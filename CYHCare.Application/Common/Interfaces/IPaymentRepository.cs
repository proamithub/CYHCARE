using CYHCare.Application.DTOs;

namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Payment tracking (via Stored Procedures).
/// </summary>
public interface IPaymentRepository
{
    /// <summary>
    /// Calls sp_Payment_GetByHelper to fetch helper earnings.
    /// </summary>
    Task<IEnumerable<HelperEarningsDto>> GetByHelperAsync(int helperId);

    /// <summary>
    /// Calls sp_Payment_MarkPaid to mark payout as settled.
    /// </summary>
    Task<PaymentDto?> MarkPaidAsync(int paymentId, int? adminUserId = null);
}
