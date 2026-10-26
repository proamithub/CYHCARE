namespace CYHCare.Domain.Entities;

public class HelperProfile
{
    public int HelperId { get; set; }
    public int UserId { get; set; }
    public string KycStatus { get; set; } = "Pending";
    public bool IsOnDuty { get; set; }
    public bool LocationSharing { get; set; } = true;
    public decimal? LastLat { get; set; }
    public decimal? LastLng { get; set; }
    public DateTime? LastUpdate { get; set; }
    public decimal HourlyRate { get; set; } = 100.00m;
    public decimal Rating { get; set; } = 0.00m;

    // Joined User Details
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool? IsVerified { get; set; }
}
