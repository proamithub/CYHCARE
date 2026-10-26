namespace CYHCare.Application.DTOs;

public class CreateJobRequestDto
{
    public string ServiceType { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public DateTime ScheduledAt { get; set; }
}

public class JobResponseDto
{
    public int JobId { get; set; }
    public int RequesterId { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }

    // Linked Assignment
    public int? AssignmentId { get; set; }
    public int? HelperId { get; set; }
    public string? HelperName { get; set; }
    public string? HelperPhone { get; set; }
    public decimal? HelperRating { get; set; }
    public decimal? HelperLastLat { get; set; }
    public decimal? HelperLastLng { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    // Linked Requester
    public string? RequesterName { get; set; }
    public string? RequesterPhone { get; set; }

    // Linked Payment
    public decimal? PaymentAmount { get; set; }
    public string? PaymentStatus { get; set; }
}

public class AcceptJobResponseDto
{
    public int AssignmentId { get; set; }
    public int JobId { get; set; }
    public int HelperId { get; set; }
    public DateTime AcceptedAt { get; set; }
    public string JobStatus { get; set; } = "Accepted";
    public int RequesterId { get; set; }
}

public class UpdateJobStatusRequestDto
{
    public string NewStatus { get; set; } = string.Empty; // OnTheWay, Arrived, InProgress, Cancelled
    public decimal? CurrentLat { get; set; } // For Geofence validation on Arrived
    public decimal? CurrentLng { get; set; }
}

public class CompleteJobResponseDto
{
    public int PaymentId { get; set; }
    public int AssignmentId { get; set; }
    public int JobId { get; set; }
    public int WorkingMinutes { get; set; }
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public DateTime CompletedAt { get; set; }
}
