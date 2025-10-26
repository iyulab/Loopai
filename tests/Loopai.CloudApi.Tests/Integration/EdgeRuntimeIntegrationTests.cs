using System.Text.Json;
using Loopai.CloudApi.Tests.Mocks;
using Xunit;

namespace Loopai.CloudApi.Tests.Integration;

/// <summary>
/// Integration tests for edge runtime execution.
/// </summary>
public class EdgeRuntimeIntegrationTests
{
    private readonly MockEdgeRuntimeService _mockRuntime;

    public EdgeRuntimeIntegrationTests()
    {
        _mockRuntime = new MockEdgeRuntimeService();
    }

    [Fact]
    public async Task ExecuteAsync_SimpleProgram_ReturnsOutput()
    {
        // Arrange
        var code = "async function main(input) { return { result: input.value * 2 }; }";
        var input = JsonDocument.Parse("{\"value\": 21}");
        var expectedOutput = JsonDocument.Parse("{\"result\": 42}");

        _mockRuntime.ConfigureExecutor(code, _ => expectedOutput);

        // Act
        var result = await _mockRuntime.ExecuteAsync(code, "typescript", input);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Output);
        Assert.Equal(42, result.Output.RootElement.GetProperty("result").GetInt32());
        Assert.True(result.ExecutionTimeMs >= 0);
        Assert.True(result.MemoryUsedBytes > 0);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_ReturnsTimeoutError()
    {
        // Arrange
        var code = "while(true) {}"; // Infinite loop
        var input = JsonDocument.Parse("{}");

        _mockRuntime.ConfigureTimeout();

        // Act
        var result = await _mockRuntime.ExecuteAsync(code, "typescript", input, timeoutMs: 1000);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Output);
        Assert.NotNull(result.Error);
        Assert.Contains("timeout", result.Error.ToLower());
    }

    [Fact]
    public async Task ExecuteAsync_RuntimeError_ReturnsError()
    {
        // Arrange
        var code = "throw new Error('Division by zero');";
        var input = JsonDocument.Parse("{}");

        _mockRuntime.ConfigureFailure("Division by zero");

        // Act
        var result = await _mockRuntime.ExecuteAsync(code, "typescript", input);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Output);
        Assert.NotNull(result.Error);
        Assert.Contains("Division by zero", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ComplexInput_ProcessesCorrectly()
    {
        // Arrange
        var code = "complex-processor";
        var input = JsonDocument.Parse("""
        {
            "user": {
                "id": 123,
                "name": "Test User"
            },
            "items": [1, 2, 3, 4, 5],
            "metadata": {
                "timestamp": "2025-01-26T10:00:00Z"
            }
        }
        """);

        var expectedOutput = JsonDocument.Parse("""
        {
            "processed": true,
            "user_id": 123,
            "item_count": 5,
            "sum": 15
        }
        """);

        _mockRuntime.ConfigureExecutor(code, inputDoc =>
        {
            var items = inputDoc.RootElement.GetProperty("items");
            var sum = 0;
            foreach (var item in items.EnumerateArray())
            {
                sum += item.GetInt32();
            }

            return JsonDocument.Parse($$"""
            {
                "processed": true,
                "user_id": {{inputDoc.RootElement.GetProperty("user").GetProperty("id").GetInt32()}},
                "item_count": {{items.GetArrayLength()}},
                "sum": {{sum}}
            }
            """);
        });

        // Act
        var result = await _mockRuntime.ExecuteAsync(code, "typescript", input);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Output);
        Assert.True(result.Output.RootElement.GetProperty("processed").GetBoolean());
        Assert.Equal(123, result.Output.RootElement.GetProperty("user_id").GetInt32());
        Assert.Equal(5, result.Output.RootElement.GetProperty("item_count").GetInt32());
        Assert.Equal(15, result.Output.RootElement.GetProperty("sum").GetInt32());
    }

    [Fact]
    public async Task ExecuteAsync_WithDelay_RecordsExecutionTime()
    {
        // Arrange
        var code = "async function main(input) { return input; }";
        var input = JsonDocument.Parse("{\"test\": true}");
        var delay = 100;

        _mockRuntime.ConfigureDelay(delay);

        // Act
        var result = await _mockRuntime.ExecuteAsync(code, "typescript", input);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.ExecutionTimeMs >= delay);
    }

    [Fact]
    public async Task ExecuteAsync_DefaultBehavior_PassesInputThrough()
    {
        // Arrange - no configured executor
        var code = "unknown-program";
        var input = JsonDocument.Parse("{\"value\": 42, \"name\": \"test\"}");

        // Act
        var result = await _mockRuntime.ExecuteAsync(code, "typescript", input);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Output);
        Assert.True(result.Output.RootElement.GetProperty("processed").GetBoolean());

        // Check input is embedded in output
        var embedded = result.Output.RootElement.GetProperty("input");
        Assert.Equal(42, embedded.GetProperty("value").GetInt32());
        Assert.Equal("test", embedded.GetProperty("name").GetString());
    }

    [Theory]
    [InlineData("typescript", "ts")]
    [InlineData("javascript", "js")]
    [InlineData("python", "py")]
    public async Task ExecuteAsync_DifferentLanguages_ExecutesCorrectly(string language, string _)
    {
        // Arrange
        var code = $"// {language} program";
        var input = JsonDocument.Parse("{\"lang\": \"" + language + "\"}");

        // Act
        var result = await _mockRuntime.ExecuteAsync(code, language, input);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Output);
    }
}
