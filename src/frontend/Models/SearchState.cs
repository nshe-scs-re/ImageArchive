using System.Text.Json.Serialization;

namespace frontend.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SearchState
{
    NotSubmitted = 0,
    Loading = 1,
    Loaded =2
}