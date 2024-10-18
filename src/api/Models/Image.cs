﻿using System.ComponentModel.DataAnnotations;

namespace api.Models;

/// <summary>
/// Models the 'Images' table in the 'ImageDatabase' database for use with Entity Framework Core
/// </summary>
public class Image
{
    [Key]
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? FilePath { get; set; }
    public DateTime DateTime { get; set; }
    public long UnixTime { get; set; }
}
