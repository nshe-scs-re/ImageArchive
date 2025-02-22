namespace api.Models;

public class ImageUploadItem
{
    public IFormFile File { get; set; }
    public long? UnixTime { get; set; }
    public DateTime? DateTime { get; set; }
    public string? SiteName { get; set; }
    public int? SiteNumber { get; set; }
    public int? CameraPositionNumber { get; set; }
    public string? CameraPositionName { get; set; }
}