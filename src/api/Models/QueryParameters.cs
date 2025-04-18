namespace api.Models;

public class QueryParameters
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SiteName { get; set; }
    public int? SiteNumber { get; set; }
    public int? CameraPosition { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}