namespace CYHCare.Application.DTOs;

public class AdminLiveSummaryDto
{
    public int ActiveJobsCount { get; set; }
    public int PendingJobsCount { get; set; }
    public int ArrivedJobsCount { get; set; }
    public int InProgressJobsCount { get; set; }
    public int OnDutyHelpersCount { get; set; }
    public int PendingKycCount { get; set; }
    public int CompletedTodayCount { get; set; }
}

public class AdminActiveJobDto
{
    public int JobId { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal JobLat { get; set; }
    public decimal JobLng { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterPhone { get; set; } = string.Empty;
    public int? AssignmentId { get; set; }
    public int? HelperId { get; set; }
    public string? HelperName { get; set; }
    public string? HelperPhone { get; set; }
    public decimal? HelperLastLat { get; set; }
    public decimal? HelperLastLng { get; set; }
    public DateTime? HelperLastUpdate { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? StartedAt { get; set; }
}
