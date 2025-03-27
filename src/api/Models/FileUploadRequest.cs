using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class FileUploadItem
{
    [Required]
    public IFormFile File { get; set; }
    [Required]
    public DateTime DateTime { get; set; }
    public long UnixTime { get; set; }

    [Required]
    public string SiteName { get; set; }

    [Required]
    public int SiteNumber { get; set; }

    [Required]
    public int CameraPositionNumber { get; set; }

    [Required]
    public string CameraPositionName { get; set; }

    public override string ToString()
    {
        return $"File: {File?.Name}, DateTime: {DateTime}, SiteName: {SiteName}, SiteNumber: {SiteNumber}, CameraPositionNumber: {CameraPositionNumber}, CameraPositionName: {CameraPositionName}";
    }
}

public class FileUploadRequest
{
    [Required]
    public List<FileUploadItem> FileItems { get; set; }
}


