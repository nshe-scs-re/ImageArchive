using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace frontend.Models;

public class FileUploadItem
{
    [Required]
    public IBrowserFile File { get; set; }
    [Required]
    public DateTime? DateTime { get; set; }
    public long? UnixTime { get; set; }
    [Required]
    public string? SiteName { get; set; }
    [Required]
    public int? SiteNumber { get; set; }
    [Required]
    public int? CameraPositionNumber { get; set; }
    [Required]
    public string? CameraPositionName { get; set; }

    public override string ToString()
    {
        return $"File: {File?.Name}, DateTime: {DateTime}, SiteName: {SiteName}, SiteNumber: {SiteNumber}, CameraPositionNumber: {CameraPositionNumber}, CameraPositionName: {CameraPositionName}";
    }
}

public class FileUploadRequest
{
    public List<FileUploadItem> FileItems { get; set; }

}