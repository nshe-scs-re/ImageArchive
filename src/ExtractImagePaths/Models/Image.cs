namespace ExtractImagePaths.Models
{
    public class Image
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public DateTime DateTime { get; set; }
        public long UnixTime { get; set; }
        public string? SiteName {  get; set; }
        public int? SiteNumber { get; set; }
        public int CameraNumber {  get; set; }
        public int? CameraPositionNumber { get; set; }
        public string? CameraPositionName { get; set; }
    }
}
