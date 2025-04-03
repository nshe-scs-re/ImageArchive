namespace frontend.Models;

public class ArchiveRequest
{
    public Guid Id { get; set; }
    public string? FilePath { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? SiteName { get; set; }
    public int? SiteNumber { get; set; }
    public int? CameraPositionNumber { get; set; }
    public ArchiveStatus Status { get; set; }
    public List<string>? ExceptionMessages { get; set; }
    public void AddError(string message)
    {
        ExceptionMessages ??= new List<string>();

        ExceptionMessages.Add(message);
        Status = ArchiveStatus.Failed;
    }
}
