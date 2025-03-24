﻿using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class FileUploadItem
{
    public IFormFile? File { get; set; }
    public DateTime? DateTime { get; set; }
    public long? UnixTime { get; set; }
    public string? SiteName { get; set; }
    public int? SiteNumber { get; set; }
    public int? CameraPositionNumber { get; set; }
    public string? CameraPositionName { get; set; }

    public override string ToString()
    {
        return $"File: {File?.Name}, DateTime: {DateTime}, SiteName: {SiteName}, SiteNumber: {SiteNumber}, CameraPositionNumber: {CameraPositionNumber}, CameraPositionName: {CameraPositionName}";
    }
}

public class FileUploadRequest
{
    public List<FileUploadItem>? FileItems { get; set; }
}
