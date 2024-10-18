namespace frontend.Models;

/// <summary>
/// Model to bind form data.
/// </summary>
public class ImageQuery
{
    public long? Id { get; set; }
    public string? Name { get; set; } = null!;
    public string? FilePath { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public TimeOnly StartTimeOnly { get; set; }
    public TimeOnly EndTimeOnly { get; set; }
    public long? UnixTime { get; set; }
    public string? Site {  get; set; }
    public int? CameraNumber { get; set; }
    public int? CameraPosition { get; set; }
}
