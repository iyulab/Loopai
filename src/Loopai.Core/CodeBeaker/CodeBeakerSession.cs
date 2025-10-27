namespace Loopai.Core.CodeBeaker;

/// <summary>
/// Represents a CodeBeaker session.
/// </summary>
public class CodeBeakerSession
{
    public required string SessionId { get; init; }
    public required string ContainerId { get; init; }
    public required string Language { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime LastActivity { get; set; }
    public SessionState State { get; set; }
    public int ExecutionCount { get; set; }
    public SemaphoreSlim ExecutionLock { get; } = new(1, 1);

    public bool IsExpired(TimeSpan idleTimeout, TimeSpan maxLifetime, DateTime now)
    {
        var idleTime = now - LastActivity;
        var lifetime = now - CreatedAt;

        return idleTime > idleTimeout || lifetime > maxLifetime;
    }

    public void UpdateActivity()
    {
        LastActivity = DateTime.UtcNow;
        State = SessionState.Active;
    }

    public void MarkIdle()
    {
        State = SessionState.Idle;
    }
}

/// <summary>
/// Session state enumeration.
/// </summary>
public enum SessionState
{
    Creating,
    Active,
    Idle,
    Closing,
    Closed
}
