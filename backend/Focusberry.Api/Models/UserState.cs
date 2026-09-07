namespace Focusberry.Api.Models;

public sealed class UserState
{
    public Guid UserId { get; set; }
    public string Data { get; set; } = "{}";
    public long Version { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
