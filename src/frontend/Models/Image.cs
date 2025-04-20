namespace frontend.Models;

/// <summary>
/// Models the 'Images' table in the 'ImageDatabase' database for use with Entity Framework Core
/// </summary>
public class Image
{
    public long Id { get; set; }
    public string? FilePath { get; set; }
    public long? UnixTime { get; set; }
    public DateTime? DateTime { get; set; }
    public string? SiteName { get; set; }
    public int? SiteNumber { get; set; }
    public int? CameraPositionNumber { get; set; }
    public string? CameraPositionName { get; set; }
    public string? WeatherPrediction { get; set; }
    public double? WeatherPredictionPercent { get; set; }
    public string? SnowPrediction { get; set; }
    public double? SnowPredictionPercent { get; set; }
}