namespace frontend.Models;

public class UserQuery
{
    public long QueryId { get; set; }

    public required string UserId { get; set; }

    public required string Parameters { get; set; }

    public DateTime Timestamp { get; set; }
}
