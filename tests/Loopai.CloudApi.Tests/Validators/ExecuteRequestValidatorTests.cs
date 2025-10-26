using System.Text.Json;
using FluentAssertions;
using FluentValidation.TestHelper;
using Loopai.CloudApi.DTOs;
using Loopai.CloudApi.Validators;

namespace Loopai.CloudApi.Tests.Validators;

public class ExecuteRequestValidatorTests
{
    private readonly ExecuteRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Request_Is_Valid()
    {
        // Arrange
        var request = new ExecuteRequest
        {
            TaskId = Guid.NewGuid(),
            Input = JsonDocument.Parse("{\"value\": 42}")
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_TaskId_Is_Empty()
    {
        // Arrange
        var request = new ExecuteRequest
        {
            TaskId = Guid.Empty,
            Input = JsonDocument.Parse("{\"value\": 42}")
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TaskId)
              .WithErrorMessage("Task ID is required");
    }

    [Fact]
    public void Should_Fail_When_Input_Is_Null()
    {
        // Arrange
        var request = new ExecuteRequest
        {
            TaskId = Guid.NewGuid(),
            Input = null!
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Input)
              .WithErrorMessage("Input data is required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Should_Fail_When_Version_Is_Not_Positive(int version)
    {
        // Arrange
        var request = new ExecuteRequest
        {
            TaskId = Guid.NewGuid(),
            Version = version,
            Input = JsonDocument.Parse("{\"value\": 42}")
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Version)
              .WithErrorMessage("Version must be greater than 0");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void Should_Pass_When_Version_Is_Positive(int version)
    {
        // Arrange
        var request = new ExecuteRequest
        {
            TaskId = Guid.NewGuid(),
            Version = version,
            Input = JsonDocument.Parse("{\"value\": 42}")
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Version);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(60001)]
    public void Should_Fail_When_TimeoutMs_Is_Invalid(int timeoutMs)
    {
        // Arrange
        var request = new ExecuteRequest
        {
            TaskId = Guid.NewGuid(),
            TimeoutMs = timeoutMs,
            Input = JsonDocument.Parse("{\"value\": 42}")
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TimeoutMs)
              .WithErrorMessage("Timeout must be between 1 and 60000 milliseconds");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5000)]
    [InlineData(60000)]
    public void Should_Pass_When_TimeoutMs_Is_Valid(int timeoutMs)
    {
        // Arrange
        var request = new ExecuteRequest
        {
            TaskId = Guid.NewGuid(),
            TimeoutMs = timeoutMs,
            Input = JsonDocument.Parse("{\"value\": 42}")
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TimeoutMs);
    }

    [Fact]
    public void Should_Pass_When_All_Optional_Fields_Are_Provided()
    {
        // Arrange
        var request = new ExecuteRequest
        {
            TaskId = Guid.NewGuid(),
            Version = 5,
            Input = JsonDocument.Parse("{\"value\": 42}"),
            ForceValidation = true,
            TimeoutMs = 10000
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
