using System.Text.Json;
using FluentAssertions;
using FluentValidation.TestHelper;
using Loopai.CloudApi.DTOs;
using Loopai.CloudApi.Validators;

namespace Loopai.CloudApi.Tests.Validators;

public class CreateTaskRequestValidatorTests
{
    private readonly CreateTaskRequestValidator _validator = new();

    private static CreateTaskRequest CreateValidRequest() => new()
    {
        Name = "test_task",
        Description = "Test task description",
        InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
        OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}")
    };

    [Fact]
    public void Should_Pass_When_Request_Is_Valid()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Should_Fail_When_Name_Is_Empty(string? name)
    {
        // Arrange
        var request = CreateValidRequest() with { Name = name! };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Task name is required");
    }

    [Fact]
    public void Should_Fail_When_Name_Exceeds_Maximum_Length()
    {
        // Arrange
        var request = CreateValidRequest() with { Name = new string('a', 201) };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Task name must not exceed 200 characters");
    }

    [Theory]
    [InlineData("task name with spaces")]
    [InlineData("task@name")]
    [InlineData("task#name")]
    [InlineData("task!name")]
    public void Should_Fail_When_Name_Contains_Invalid_Characters(string name)
    {
        // Arrange
        var request = CreateValidRequest() with { Name = name };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Task name must contain only alphanumeric characters, underscores, hyphens, and periods");
    }

    [Theory]
    [InlineData("valid_task")]
    [InlineData("task-name")]
    [InlineData("task.name")]
    [InlineData("TaskName123")]
    [InlineData("task_name_123")]
    public void Should_Pass_When_Name_Contains_Valid_Characters(string name)
    {
        // Arrange
        var request = CreateValidRequest() with { Name = name };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Should_Fail_When_Description_Is_Empty(string? description)
    {
        // Arrange
        var request = CreateValidRequest() with { Description = description! };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Task description is required");
    }

    [Fact]
    public void Should_Fail_When_Description_Exceeds_Maximum_Length()
    {
        // Arrange
        var request = CreateValidRequest() with { Description = new string('a', 5001) };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Task description must not exceed 5000 characters");
    }

    [Fact]
    public void Should_Fail_When_InputSchema_Is_Null()
    {
        // Arrange
        var request = CreateValidRequest() with { InputSchema = null! };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InputSchema)
              .WithErrorMessage("Input schema is required");
    }

    [Fact]
    public void Should_Fail_When_OutputSchema_Is_Null()
    {
        // Arrange
        var request = CreateValidRequest() with { OutputSchema = null! };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OutputSchema)
              .WithErrorMessage("Output schema is required");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void Should_Fail_When_AccuracyTarget_Is_Out_Of_Range(double accuracyTarget)
    {
        // Arrange
        var request = CreateValidRequest() with { AccuracyTarget = accuracyTarget };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccuracyTarget)
              .WithErrorMessage("Accuracy target must be between 0.0 and 1.0");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Should_Pass_When_AccuracyTarget_Is_In_Range(double accuracyTarget)
    {
        // Arrange
        var request = CreateValidRequest() with { AccuracyTarget = accuracyTarget };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AccuracyTarget);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(60001)]
    public void Should_Fail_When_LatencyTargetMs_Is_Out_Of_Range(int latencyTargetMs)
    {
        // Arrange
        var request = CreateValidRequest() with { LatencyTargetMs = latencyTargetMs };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LatencyTargetMs)
              .WithErrorMessage("Latency target must be between 1 and 60000 milliseconds");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    [InlineData(60000)]
    public void Should_Pass_When_LatencyTargetMs_Is_In_Range(int latencyTargetMs)
    {
        // Arrange
        var request = CreateValidRequest() with { LatencyTargetMs = latencyTargetMs };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LatencyTargetMs);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void Should_Fail_When_SamplingRate_Is_Out_Of_Range(double samplingRate)
    {
        // Arrange
        var request = CreateValidRequest() with { SamplingRate = samplingRate };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SamplingRate)
              .WithErrorMessage("Sampling rate must be between 0.0 and 1.0");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.1)]
    [InlineData(1.0)]
    public void Should_Pass_When_SamplingRate_Is_In_Range(double samplingRate)
    {
        // Arrange
        var request = CreateValidRequest() with { SamplingRate = samplingRate };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SamplingRate);
    }

    [Fact]
    public void Should_Fail_When_Examples_Is_Null()
    {
        // Arrange
        var request = CreateValidRequest() with { Examples = null! };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Examples)
              .WithErrorMessage("Examples list cannot be null (use empty list if no examples)");
    }

    [Fact]
    public void Should_Pass_When_Examples_Is_Empty_List()
    {
        // Arrange
        var request = CreateValidRequest() with { Examples = Array.Empty<JsonDocument>() };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Examples);
    }

    [Fact]
    public void Should_Pass_When_All_Optional_Fields_Are_Provided()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Name = "test_task",
            Description = "Test task description",
            InputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            OutputSchema = JsonDocument.Parse("{\"type\": \"object\"}"),
            Examples = new[] { JsonDocument.Parse("{\"input\": 1, \"output\": 2}") },
            AccuracyTarget = 0.95,
            LatencyTargetMs = 5000,
            SamplingRate = 0.2
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
