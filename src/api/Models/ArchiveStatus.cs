using System.Text.Json.Serialization;

namespace api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ArchiveStatus
{
    None = 0,
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Canceled = 4,
    Failed = 5
}