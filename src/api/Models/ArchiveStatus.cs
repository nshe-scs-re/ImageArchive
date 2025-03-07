using System.Text.Json.Serialization;

namespace api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ArchiveStatus
{
    Failed = -1,
    Unknown = 0,
    Pending = 1,
    Canceled = 2,
    Processing = 3,
    Completed = 4
}