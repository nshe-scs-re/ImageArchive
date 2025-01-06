namespace api.Models;

public class ArchiveRequest
{
    public Guid Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ArchiveStatus Status { get; set; } = ArchiveStatus.Unknown;
    public List<string> ExceptionMessages { get; set; } = new List<string>();

    public string? FilePath { get; set; }

    public void AddError(string message)
    {
        ExceptionMessages.Add(message);
        Status = ArchiveStatus.Failed;
    }
}

