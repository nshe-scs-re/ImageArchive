namespace frontend.Models;

/// <summary>
/// Model to bind form data.
/// </summary>
public class ImageQuery
{
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? SiteName { get; set; }
    public int SiteNumber { get; set; }
    public int CameraPositionNumber { get; set; }
}
