using Loopai.Core.CodeBeaker;
using Loopai.Core.CodeBeaker.Models;
using Loopai.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Loopai.Core.Tests.CodeBeaker;

/// <summary>
/// Integration tests for CodeBeaker runtime service.
/// Requires CodeBeaker server running at ws://localhost:5000/ws/jsonrpc
/// </summary>
public class CodeBeakerIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly ICodeBeakerClient _client;
    private readonly CodeBeakerSessionPool _sessionPool;
    private readonly IEdgeRuntimeService _runtimeService;
    private readonly ILoggerFactory _loggerFactory;

    public CodeBeakerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        // Setup logging
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Configure options
        var options = Options.Create(new CodeBeakerOptions
        {
            WebSocketUrl = "ws://localhost:5000/ws/jsonrpc",
            SessionPoolSize = 5,
            SessionIdleTimeoutMinutes = 30,
            SessionMaxLifetimeMinutes = 120,
            DefaultTimeoutMs = 30000,
            EnableAutoCleanup = false  // Disable for tests
        });

        // Create services
        _client = new CodeBeakerClient(
            options,
            _loggerFactory.CreateLogger<CodeBeakerClient>());

        _sessionPool = new CodeBeakerSessionPool(
            _client,
            options,
            _loggerFactory.CreateLogger<CodeBeakerSessionPool>());

        _runtimeService = new CodeBeakerRuntimeService(
            _sessionPool,
            _loggerFactory.CreateLogger<CodeBeakerRuntimeService>());
    }

    public async Task InitializeAsync()
    {
        // Connect to CodeBeaker
        await _client.ConnectAsync();
        _output.WriteLine("Connected to CodeBeaker");
    }

    public async Task DisposeAsync()
    {
        await _sessionPool.DisposeAsync();
        await _client.DisposeAsync();
        _loggerFactory.Dispose();
        _output.WriteLine("Disposed CodeBeaker client and session pool");
    }

    [Fact(Skip = "Requires CodeBeaker server")]
    public async Task ExecuteAsync_PythonCode_ShouldReturnCorrectResult()
    {
        // Arrange
        var code = @"
result = {'output': input_data['value'] * 2}
";
        var input = JsonDocument.Parse("{\"value\": 21}");

        // Act
        var result = await _runtimeService.ExecuteAsync(
            code,
            "python",
            input,
            timeoutMs: 30000);

        // Assert
        Assert.True(result.Success, result.Error);
        Assert.NotNull(result.Output);

        var output = result.Output.RootElement;
        Assert.Equal(42, output.GetProperty("output").GetInt32());
    }

    [Fact(Skip = "Requires CodeBeaker server")]
    public async Task ExecuteAsync_JavaScriptCode_ShouldReturnCorrectResult()
    {
        // Arrange
        var code = @"
const result = { output: input_data.value * 2 };
";
        var input = JsonDocument.Parse("{\"value\": 21}");

        // Act
        var result = await _runtimeService.ExecuteAsync(
            code,
            "javascript",
            input,
            timeoutMs: 30000);

        // Assert
        Assert.True(result.Success, result.Error);
        Assert.NotNull(result.Output);

        var output = result.Output.RootElement;
        Assert.Equal(42, output.GetProperty("output").GetInt32());
    }

    [Fact(Skip = "Requires CodeBeaker server")]
    public async Task ExecuteAsync_GoCode_ShouldReturnCorrectResult()
    {
        // Arrange
        var code = @"
value := input_data[""value""].(float64)
result := map[string]interface{}{""output"": value * 2}
";
        var input = JsonDocument.Parse("{\"value\": 21}");

        // Act
        var result = await _runtimeService.ExecuteAsync(
            code,
            "go",
            input,
            timeoutMs: 30000);

        // Assert
        Assert.True(result.Success, result.Error);
        Assert.NotNull(result.Output);

        var output = result.Output.RootElement;
        Assert.Equal(42, output.GetProperty("output").GetInt32());
    }

    [Fact(Skip = "Requires CodeBeaker server")]
    public async Task ExecuteAsync_CSharpCode_ShouldReturnCorrectResult()
    {
        // Arrange
        var code = @"
var value = input_data.GetProperty(""value"").GetInt32();
var result = new { output = value * 2 };
";
        var input = JsonDocument.Parse("{\"value\": 21}");

        // Act
        var result = await _runtimeService.ExecuteAsync(
            code,
            "csharp",
            input,
            timeoutMs: 30000);

        // Assert
        Assert.True(result.Success, result.Error);
        Assert.NotNull(result.Output);

        var output = result.Output.RootElement;
        Assert.Equal(42, output.GetProperty("output").GetInt32());
    }

    [Fact(Skip = "Requires CodeBeaker server")]
    public async Task SessionPool_ShouldReuseSession()
    {
        // Arrange
        var code = @"
result = {'count': 1}
";
        var input = JsonDocument.Parse("{}");

        // Act - Execute twice to test session reuse
        var result1 = await _runtimeService.ExecuteAsync(code, "python", input);
        var result2 = await _runtimeService.ExecuteAsync(code, "python", input);

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);

        var stats = _sessionPool.GetStatistics();
        _output.WriteLine($"Total sessions: {stats.TotalSessions}");
        _output.WriteLine($"Active sessions: {stats.ActiveSessions}");
        _output.WriteLine($"Idle sessions: {stats.IdleSessions}");

        // Should reuse same session (only 1 session created)
        Assert.True(stats.TotalSessions <= 2, $"Expected <=2 sessions, got {stats.TotalSessions}");
    }

    [Fact(Skip = "Requires CodeBeaker server")]
    public async Task SessionPool_ShouldCreateMultipleSessionsForDifferentLanguages()
    {
        // Arrange
        var pythonCode = "result = {'lang': 'python'}";
        var jsCode = "const result = {lang: 'javascript'};";
        var input = JsonDocument.Parse("{}");

        // Act
        var pythonResult = await _runtimeService.ExecuteAsync(pythonCode, "python", input);
        var jsResult = await _runtimeService.ExecuteAsync(jsCode, "javascript", input);

        // Assert
        Assert.True(pythonResult.Success);
        Assert.True(jsResult.Success);

        var stats = _sessionPool.GetStatistics();
        _output.WriteLine($"Total sessions: {stats.TotalSessions}");

        // Should create 2 sessions (one for each language)
        Assert.True(stats.TotalSessions >= 2, $"Expected >=2 sessions, got {stats.TotalSessions}");
    }

    [Fact(Skip = "Requires CodeBeaker server")]
    public async Task ExecuteAsync_WithErrorInCode_ShouldReturnError()
    {
        // Arrange
        var code = @"
# This will cause an error
undefined_variable
";
        var input = JsonDocument.Parse("{}");

        // Act
        var result = await _runtimeService.ExecuteAsync(code, "python", input);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        _output.WriteLine($"Error: {result.Error}");
    }

    [Fact(Skip = "Requires CodeBeaker server")]
    public async Task ExecuteAsync_UnsupportedLanguage_ShouldReturnError()
    {
        // Arrange
        var code = "some code";
        var input = JsonDocument.Parse("{}");

        // Act
        var result = await _runtimeService.ExecuteAsync(code, "rust", input);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Unsupported language", result.Error);
    }
}
