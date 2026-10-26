namespace CYHCare.Domain.Entities;

public class JobAssignment
{
    public int AssignmentId { get; set; }
    public int JobId { get; set; }
    public int HelperId { get; set; }
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ArrivedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    // Joined fields
    public string? HelperName { get; set; }
    public string? HelperPhone { get; set; }
    public decimal? HelperRating { get; set; }
    public decimal? HelperLastLat { get; set; }
    public decimal? HelperLastLng { get; set; }
}
