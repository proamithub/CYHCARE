namespace CYHCare.Domain.Entities;

public class GpsLog
{
    public long LogId { get; set; }
    public int AssignmentId { get; set; }
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public decimal? SpeedKmph { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
