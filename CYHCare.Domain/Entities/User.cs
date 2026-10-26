namespace CYHCare.Domain.Entities;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Helper specific navigation / join properties when returned from SP
    public int? HelperId { get; set; }
    public string? KycStatus { get; set; }
    public bool? IsOnDuty { get; set; }
    public bool? LocationSharing { get; set; }
}
