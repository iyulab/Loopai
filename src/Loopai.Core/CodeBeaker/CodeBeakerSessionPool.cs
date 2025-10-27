using Loopai.Core.CodeBeaker.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Loopai.Core.CodeBeaker;

/// <summary>
/// Session pool manager for CodeBeaker sessions with lifecycle management.
/// </summary>
public class CodeBeakerSessionPool : IAsyncDisposable
{
    private readonly ICodeBeakerClient _client;
    private readonly CodeBeakerOptions _options;
    private readonly ILogger<CodeBeakerSessionPool> _logger;
    private readonly ConcurrentDictionary<string, CodeBeakerSession> _sessions;
    private readonly SemaphoreSlim _poolLock;
    private readonly Timer? _cleanupTimer;
    private bool _disposed;

    public CodeBeakerSessionPool(
        ICodeBeakerClient client,
        IOptions<CodeBeakerOptions> options,
        ILogger<CodeBeakerSessionPool> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
        _sessions = new ConcurrentDictionary<string, CodeBeakerSession>();
        _poolLock = new SemaphoreSlim(_options.SessionPoolSize, _options.SessionPoolSize);

        if (_options.EnableAutoCleanup)
        {
            var cleanupInterval = TimeSpan.FromMinutes(_options.CleanupIntervalMinutes);
            _cleanupTimer = new Timer(
                async _ => await CleanupExpiredSessionsAsync(),
                null,
                cleanupInterval,
                cleanupInterval);
        }
    }

    /// <summary>
    /// Acquire a session for the specified language.
    /// Reuses existing idle session or creates new one if available.
    /// </summary>
    public async Task<CodeBeakerSession> AcquireSessionAsync(
        string language,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CodeBeakerSessionPool));
        }

        // Map language to CodeBeaker language
        if (!_options.LanguageMapping.TryGetValue(language.ToLowerInvariant(), out var codeBeakerLanguage))
        {
            codeBeakerLanguage = language;
        }

        _logger.LogDebug("Acquiring session for language: {Language}", codeBeakerLanguage);

        // Try to find existing idle session for this language
        var idleSession = _sessions.Values
            .FirstOrDefault(s => s.Language == codeBeakerLanguage && s.State == SessionState.Idle);

        if (idleSession != null)
        {
            await idleSession.ExecutionLock.WaitAsync(cancellationToken);
            try
            {
                // Double-check state after acquiring lock
                if (idleSession.State == SessionState.Idle)
                {
                    idleSession.UpdateActivity();
                    _logger.LogDebug("Reusing existing session {SessionId}", idleSession.SessionId);
                    return idleSession;
                }
            }
            catch
            {
                idleSession.ExecutionLock.Release();
                throw;
            }
        }

        // No idle session available, wait for pool slot and create new session
        await _poolLock.WaitAsync(cancellationToken);
        try
        {
            var session = await CreateSessionInternalAsync(codeBeakerLanguage, cancellationToken);
            await session.ExecutionLock.WaitAsync(cancellationToken);
            return session;
        }
        catch
        {
            _poolLock.Release();
            throw;
        }
    }

    /// <summary>
    /// Release a session back to the pool.
    /// </summary>
    public void ReleaseSession(CodeBeakerSession session)
    {
        if (_disposed)
        {
            return;
        }

        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        _logger.LogDebug("Releasing session {SessionId}", session.SessionId);

        try
        {
            session.MarkIdle();
        }
        finally
        {
            session.ExecutionLock.Release();
        }
    }

    /// <summary>
    /// Execute a command in a session.
    /// </summary>
    public async Task<CommandResult> ExecuteCommandAsync(
        CodeBeakerSession session,
        CodeBeakerCommand command,
        CancellationToken cancellationToken = default)
    {
        var executeParams = new SessionExecuteParams
        {
            SessionId = session.SessionId,
            Command = command
        };

        var result = await _client.ExecuteAsync(executeParams, cancellationToken);
        session.UpdateActivity();
        return result;
    }

    /// <summary>
    /// Get session statistics.
    /// </summary>
    public SessionPoolStatistics GetStatistics()
    {
        var now = DateTime.UtcNow;
        var sessions = _sessions.Values.ToList();

        return new SessionPoolStatistics
        {
            TotalSessions = sessions.Count,
            ActiveSessions = sessions.Count(s => s.State == SessionState.Active),
            IdleSessions = sessions.Count(s => s.State == SessionState.Idle),
            AvailableSlots = _poolLock.CurrentCount,
            AverageExecutionCount = sessions.Count > 0 ? sessions.Average(s => s.ExecutionCount) : 0,
            OldestSessionAge = sessions.Count > 0
                ? sessions.Max(s => (now - s.CreatedAt).TotalMinutes)
                : 0
        };
    }

    /// <summary>
    /// Cleanup expired sessions.
    /// </summary>
    public async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogDebug("Running session cleanup");

        var now = DateTime.UtcNow;
        var idleTimeout = TimeSpan.FromMinutes(_options.SessionIdleTimeoutMinutes);
        var maxLifetime = TimeSpan.FromMinutes(_options.SessionMaxLifetimeMinutes);

        var expiredSessions = _sessions.Values
            .Where(s => s.IsExpired(idleTimeout, maxLifetime, now) && s.State == SessionState.Idle)
            .ToList();

        if (expiredSessions.Count == 0)
        {
            _logger.LogDebug("No expired sessions to cleanup");
            return;
        }

        _logger.LogInformation("Cleaning up {Count} expired sessions", expiredSessions.Count);

        foreach (var session in expiredSessions)
        {
            try
            {
                await CloseSessionInternalAsync(session, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to close expired session {SessionId}", session.SessionId);
            }
        }
    }

    private async Task<CodeBeakerSession> CreateSessionInternalAsync(
        string language,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new session for language: {Language}", language);

        var createParams = new SessionCreateParams
        {
            Language = language,
            IdleTimeoutMinutes = _options.SessionIdleTimeoutMinutes,
            MaxLifetimeMinutes = _options.SessionMaxLifetimeMinutes,
            MemoryLimitMB = _options.MemoryLimitMB,
            CpuShares = _options.CpuShares
        };

        var result = await _client.CreateSessionAsync(createParams, cancellationToken);

        var session = new CodeBeakerSession
        {
            SessionId = result.SessionId,
            ContainerId = result.ContainerId,
            Language = result.Language,
            CreatedAt = result.CreatedAt,
            LastActivity = DateTime.UtcNow,
            State = SessionState.Active,
            ExecutionCount = 0
        };

        _sessions[session.SessionId] = session;

        _logger.LogInformation(
            "Created session {SessionId} for language {Language}",
            session.SessionId, language);

        return session;
    }

    private async Task CloseSessionInternalAsync(
        CodeBeakerSession session,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Closing session {SessionId}", session.SessionId);

        session.State = SessionState.Closing;

        try
        {
            var closeParams = new SessionCloseParams
            {
                SessionId = session.SessionId
            };

            await _client.CloseSessionAsync(closeParams, cancellationToken);

            _sessions.TryRemove(session.SessionId, out _);
            _poolLock.Release();

            session.State = SessionState.Closed;

            _logger.LogInformation("Closed session {SessionId}", session.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing session {SessionId}", session.SessionId);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _logger.LogInformation("Disposing CodeBeaker session pool");

        // Stop cleanup timer
        _cleanupTimer?.Dispose();

        // Close all sessions
        var sessions = _sessions.Values.ToList();
        foreach (var session in sessions)
        {
            try
            {
                await CloseSessionInternalAsync(session, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing session during dispose: {SessionId}", session.SessionId);
            }
        }

        _poolLock.Dispose();

        _logger.LogInformation("Disposed CodeBeaker session pool");

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Session pool statistics.
/// </summary>
public record SessionPoolStatistics
{
    public int TotalSessions { get; init; }
    public int ActiveSessions { get; init; }
    public int IdleSessions { get; init; }
    public int AvailableSlots { get; init; }
    public double AverageExecutionCount { get; init; }
    public double OldestSessionAge { get; init; }
}
