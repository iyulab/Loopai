using Loopai.Core.CodeBeaker.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Loopai.Core.CodeBeaker;

/// <summary>
/// WebSocket JSON-RPC client for CodeBeaker.
/// </summary>
public class CodeBeakerClient : ICodeBeakerClient
{
    private readonly CodeBeakerOptions _options;
    private readonly ILogger<CodeBeakerClient> _logger;
    private readonly ClientWebSocket _webSocket;
    private readonly SemaphoreSlim _sendLock;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonRpcResponse>> _pendingRequests;
    private int _requestIdCounter;
    private Task? _receiveTask;
    private CancellationTokenSource? _receiveCts;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public CodeBeakerClient(
        IOptions<CodeBeakerOptions> options,
        ILogger<CodeBeakerClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _webSocket = new ClientWebSocket();
        _sendLock = new SemaphoreSlim(1, 1);
        _pendingRequests = new ConcurrentDictionary<int, TaskCompletionSource<JsonRpcResponse>>();
        _requestIdCounter = 0;
    }

    public bool IsConnected => _webSocket.State == WebSocketState.Open;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            _logger.LogDebug("Already connected to CodeBeaker");
            return;
        }

        _logger.LogInformation("Connecting to CodeBeaker at {Url}", _options.WebSocketUrl);

        try
        {
            var uri = new Uri(_options.WebSocketUrl);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds));

            await _webSocket.ConnectAsync(uri, cts.Token);

            // Start receive loop
            _receiveCts = new CancellationTokenSource();
            _receiveTask = ReceiveLoopAsync(_receiveCts.Token);

            _logger.LogInformation("Connected to CodeBeaker successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to CodeBeaker");
            throw new InvalidOperationException($"Failed to connect to CodeBeaker: {ex.Message}", ex);
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return;
        }

        _logger.LogInformation("Disconnecting from CodeBeaker");

        try
        {
            // Cancel receive loop
            _receiveCts?.Cancel();

            if (_receiveTask != null)
            {
                await _receiveTask;
            }

            // Close WebSocket
            await _webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Client disconnect",
                cancellationToken);

            // Cancel all pending requests
            foreach (var tcs in _pendingRequests.Values)
            {
                tcs.TrySetCanceled();
            }
            _pendingRequests.Clear();

            _logger.LogInformation("Disconnected from CodeBeaker");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during disconnect");
        }
    }

    public async Task<SessionCreateResult> CreateSessionAsync(
        SessionCreateParams parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating CodeBeaker session for language: {Language}", parameters.Language);

        var request = new JsonRpcRequest
        {
            Id = Interlocked.Increment(ref _requestIdCounter),
            Method = "session.create",
            Params = parameters
        };

        var response = await SendRequestAsync(request, cancellationToken);

        if (response.Error != null)
        {
            throw new CodeBeakerException(
                $"Failed to create session: {response.Error.Message}",
                response.Error.Code);
        }

        if (response.Result == null)
        {
            throw new CodeBeakerException("Session creation returned null result");
        }

        var result = JsonSerializer.Deserialize<SessionCreateResult>(
            JsonSerializer.SerializeToUtf8Bytes(response.Result),
            JsonOptions);

        if (result == null)
        {
            throw new CodeBeakerException("Failed to deserialize session create result");
        }

        _logger.LogInformation(
            "Created CodeBeaker session {SessionId} for language {Language}",
            result.SessionId, result.Language);

        return result;
    }

    public async Task<CommandResult> ExecuteAsync(
        SessionExecuteParams parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Executing command in session {SessionId}: {CommandType}",
            parameters.SessionId, parameters.Command.GetType().Name.Replace("Command", "").ToLowerInvariant());

        var request = new JsonRpcRequest
        {
            Id = Interlocked.Increment(ref _requestIdCounter),
            Method = "session.execute",
            Params = parameters
        };

        var response = await SendRequestAsync(request, cancellationToken);

        if (response.Error != null)
        {
            throw new CodeBeakerException(
                $"Failed to execute command: {response.Error.Message}",
                response.Error.Code);
        }

        if (response.Result == null)
        {
            throw new CodeBeakerException("Command execution returned null result");
        }

        var result = JsonSerializer.Deserialize<CommandResult>(
            JsonSerializer.SerializeToUtf8Bytes(response.Result),
            JsonOptions);

        if (result == null)
        {
            throw new CodeBeakerException("Failed to deserialize command result");
        }

        _logger.LogDebug(
            "Command executed in {DurationMs}ms, success: {Success}",
            result.DurationMs, result.Success);

        return result;
    }

    public async Task<SessionCloseResult> CloseSessionAsync(
        SessionCloseParams parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Closing CodeBeaker session {SessionId}", parameters.SessionId);

        var request = new JsonRpcRequest
        {
            Id = Interlocked.Increment(ref _requestIdCounter),
            Method = "session.close",
            Params = parameters
        };

        var response = await SendRequestAsync(request, cancellationToken);

        if (response.Error != null)
        {
            throw new CodeBeakerException(
                $"Failed to close session: {response.Error.Message}",
                response.Error.Code);
        }

        if (response.Result == null)
        {
            throw new CodeBeakerException("Session close returned null result");
        }

        var result = JsonSerializer.Deserialize<SessionCloseResult>(
            JsonSerializer.SerializeToUtf8Bytes(response.Result),
            JsonOptions);

        if (result == null)
        {
            throw new CodeBeakerException("Failed to deserialize session close result");
        }

        _logger.LogInformation("Closed CodeBeaker session {SessionId}", parameters.SessionId);

        return result;
    }

    private async Task<JsonRpcResponse> SendRequestAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected to CodeBeaker");
        }

        // Create completion source for this request
        var tcs = new TaskCompletionSource<JsonRpcResponse>();
        _pendingRequests[request.Id] = tcs;

        try
        {
            // Serialize and send request
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json + "\n");

            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken);
            }
            finally
            {
                _sendLock.Release();
            }

            // Wait for response
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMilliseconds(_options.DefaultTimeoutMs));

            var registration = cts.Token.Register(() => tcs.TrySetCanceled());
            try
            {
                return await tcs.Task;
            }
            finally
            {
                registration.Dispose();
            }
        }
        finally
        {
            _pendingRequests.TryRemove(request.Id, out _);
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var messageBuilder = new StringBuilder();

        try
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (WebSocketException ex)
                {
                    _logger.LogWarning(ex, "WebSocket receive error");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket closed by server");
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                messageBuilder.Append(message);

                if (result.EndOfMessage)
                {
                    var fullMessage = messageBuilder.ToString();
                    messageBuilder.Clear();

                    ProcessMessage(fullMessage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in receive loop");
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            var response = JsonSerializer.Deserialize<JsonRpcResponse>(message, JsonOptions);
            if (response?.Id == null)
            {
                _logger.LogWarning("Received response without ID");
                return;
            }

            if (_pendingRequests.TryGetValue(response.Id.Value, out var tcs))
            {
                tcs.TrySetResult(response);
            }
            else
            {
                _logger.LogWarning("Received response for unknown request ID: {Id}", response.Id);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize JSON-RPC response");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _webSocket.Dispose();
        _sendLock.Dispose();
        _receiveCts?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Exception thrown by CodeBeaker client.
/// </summary>
public class CodeBeakerException : Exception
{
    public int? ErrorCode { get; }

    public CodeBeakerException(string message, int? errorCode = null)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public CodeBeakerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
