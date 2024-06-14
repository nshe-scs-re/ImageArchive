namespace ImageProjectFrontend.Models
{
    /// <summary>
    /// Model to bind form data to in /Table.
    /// </summary>
    public class ImageQuery
    {
        public long? Id { get; set; }
        public string? Name { get; set; } = null!;
        public string? FilePath { get; set; }
        public DateTime StartDate { get; set; } = new(new DateOnly(2010, 1, 1), new TimeOnly());
        public DateTime EndDate { get; set; } = DateTime.UtcNow;
        public long? UnixTime { get; set; }

    }
}
