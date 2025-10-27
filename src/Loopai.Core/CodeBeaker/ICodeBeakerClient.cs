using Loopai.Core.CodeBeaker.Models;

namespace Loopai.Core.CodeBeaker;

/// <summary>
/// Interface for CodeBeaker WebSocket JSON-RPC client.
/// </summary>
public interface ICodeBeakerClient : IAsyncDisposable
{
    /// <summary>
    /// Connect to CodeBeaker WebSocket endpoint.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect from CodeBeaker WebSocket endpoint.
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the client is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Create a new session.
    /// </summary>
    Task<SessionCreateResult> CreateSessionAsync(
        SessionCreateParams parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a command in a session.
    /// </summary>
    Task<CommandResult> ExecuteAsync(
        SessionExecuteParams parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Close a session.
    /// </summary>
    Task<SessionCloseResult> CloseSessionAsync(
        SessionCloseParams parameters,
        CancellationToken cancellationToken = default);
}
