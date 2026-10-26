namespace CYHCare.Application.DTOs;

public class InsertGpsLogRequestDto
{
    public int AssignmentId { get; set; }
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public decimal? SpeedKmph { get; set; }
}

public class GpsPointDto
{
    public long LogId { get; set; }
    public int AssignmentId { get; set; }
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public decimal? SpeedKmph { get; set; }
    public DateTime RecordedAt { get; set; }
}
