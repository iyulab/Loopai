using System.Text.Json.Serialization;

namespace Loopai.Core.CodeBeaker.Models;

/// <summary>
/// JSON-RPC 2.0 request.
/// </summary>
public record JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("method")]
    public required string Method { get; init; }

    [JsonPropertyName("params")]
    public object? Params { get; init; }
}

/// <summary>
/// JSON-RPC 2.0 response.
/// </summary>
public record JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("result")]
    public object? Result { get; init; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; init; }
}

/// <summary>
/// JSON-RPC 2.0 error.
/// </summary>
public record JsonRpcError
{
    [JsonPropertyName("code")]
    public required int Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("data")]
    public object? Data { get; init; }
}

/// <summary>
/// CodeBeaker session creation parameters.
/// </summary>
public record SessionCreateParams
{
    [JsonPropertyName("language")]
    public required string Language { get; init; }

    [JsonPropertyName("idleTimeoutMinutes")]
    public int IdleTimeoutMinutes { get; init; } = 30;

    [JsonPropertyName("maxLifetimeMinutes")]
    public int MaxLifetimeMinutes { get; init; } = 120;

    [JsonPropertyName("memoryLimitMB")]
    public int? MemoryLimitMB { get; init; }

    [JsonPropertyName("cpuShares")]
    public long? CpuShares { get; init; }
}

/// <summary>
/// CodeBeaker session creation result.
/// </summary>
public record SessionCreateResult
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    [JsonPropertyName("containerId")]
    public required string ContainerId { get; init; }

    [JsonPropertyName("language")]
    public required string Language { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTime CreatedAt { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("config")]
    public object? Config { get; init; }
}

/// <summary>
/// CodeBeaker session execution parameters.
/// </summary>
public record SessionExecuteParams
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    [JsonPropertyName("command")]
    public required CodeBeakerCommand Command { get; init; }
}

/// <summary>
/// Base class for CodeBeaker commands.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(WriteFileCommand), typeDiscriminator: "write_file")]
[JsonDerivedType(typeof(ReadFileCommand), typeDiscriminator: "read_file")]
[JsonDerivedType(typeof(ExecuteShellCommand), typeDiscriminator: "execute_shell")]
[JsonDerivedType(typeof(CreateDirectoryCommand), typeDiscriminator: "create_directory")]
[JsonDerivedType(typeof(DeleteFileCommand), typeDiscriminator: "delete_file")]
public abstract record CodeBeakerCommand
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

/// <summary>
/// Write file command.
/// </summary>
public record WriteFileCommand : CodeBeakerCommand
{
    public override string Type => "write_file";

    [JsonPropertyName("path")]
    public required string Path { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("mode")]
    public string Mode { get; init; } = "Create";
}

/// <summary>
/// Read file command.
/// </summary>
public record ReadFileCommand : CodeBeakerCommand
{
    public override string Type => "read_file";

    [JsonPropertyName("path")]
    public required string Path { get; init; }
}

/// <summary>
/// Execute shell command.
/// </summary>
public record ExecuteShellCommand : CodeBeakerCommand
{
    public override string Type => "execute_shell";

    [JsonPropertyName("commandName")]
    public required string CommandName { get; init; }

    [JsonPropertyName("args")]
    public List<string>? Args { get; init; }

    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; init; }
}

/// <summary>
/// Create directory command.
/// </summary>
public record CreateDirectoryCommand : CodeBeakerCommand
{
    public override string Type => "create_directory";

    [JsonPropertyName("path")]
    public required string Path { get; init; }
}

/// <summary>
/// Delete file command.
/// </summary>
public record DeleteFileCommand : CodeBeakerCommand
{
    public override string Type => "delete_file";

    [JsonPropertyName("path")]
    public required string Path { get; init; }
}

/// <summary>
/// Command execution result.
/// </summary>
public record CommandResult
{
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    [JsonPropertyName("result")]
    public object? Result { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("durationMs")]
    public required int DurationMs { get; init; }
}

/// <summary>
/// Session close parameters.
/// </summary>
public record SessionCloseParams
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }
}

/// <summary>
/// Session close result.
/// </summary>
public record SessionCloseResult
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    [JsonPropertyName("closed")]
    public required bool Closed { get; init; }
}
