namespace ImageProjectFrontend.Models;

public class ArchiveStatusResponse
{
    public enum ArchiveStatus
    {
        Failed = -1,
        Unknown,
        Pending,
        Canceled,
        Processing,
        Completed
    }
    public Guid JobId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ArchiveStatus Status { get; set; }
    public List<string>? ExceptionMessages { get; set; }
}
