namespace frontend.Models;

/// <summary>
/// Model to bind form data.
/// </summary>
public class ImageQuery
{
    public long? Id { get; set; }
    public string? FilePath { get; set; }
    public DateTime StartDateTime { get; set; } = DateTime.UnixEpoch;
    public DateTime EndDateTime { get; set; } = DateTime.Now;
    public TimeOnly StartTimeOnly { get; set; } = TimeOnly.MinValue;
    public TimeOnly EndTimeOnly { get; set; } = TimeOnly.MaxValue;
    public long? UnixTime { get; set; }
    public string? SiteName { get; set; } = "Rockland";
    public int? SiteNumber { get; set; } = 1;
    public int? CameraPosition { get; set; } = 1;
}
