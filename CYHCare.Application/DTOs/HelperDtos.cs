namespace CYHCare.Application.DTOs;

public class HelperProfileDto
{
    public int HelperId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string KycStatus { get; set; } = "Pending";
    public bool IsOnDuty { get; set; }
    public bool LocationSharing { get; set; }
    public decimal? LastLat { get; set; }
    public decimal? LastLng { get; set; }
    public DateTime? LastUpdate { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal Rating { get; set; }
}

public class PendingKycHelperDto
{
    public int HelperId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string KycStatus { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public DateTime RegisteredAt { get; set; }
}

public class ApproveKycRequestDto
{
    public int HelperId { get; set; }
    public string KycStatus { get; set; } = "Approved"; // Approved or Rejected
}

public class NearbyHelperDto
{
    public int HelperId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public decimal Rating { get; set; }
    public decimal LastLat { get; set; }
    public decimal LastLng { get; set; }
    public DateTime? LastUpdate { get; set; }
    public decimal DistanceKm { get; set; }
}
