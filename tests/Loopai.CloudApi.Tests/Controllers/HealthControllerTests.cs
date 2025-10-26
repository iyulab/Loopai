using FluentAssertions;
using Loopai.CloudApi.Controllers;
using Loopai.CloudApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Loopai.CloudApi.Tests.Controllers;

public class HealthControllerTests
{
    private readonly Mock<ILogger<HealthController>> _loggerMock;
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly LoopaiDbContext _dbContext;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _loggerMock = new Mock<ILogger<HealthController>>();
        _environmentMock = new Mock<IWebHostEnvironment>();
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Test");

        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<LoopaiDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new LoopaiDbContext(options);

        _controller = new HealthController(_loggerMock.Object, _dbContext);

        // Setup HttpContext with IWebHostEnvironment
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IWebHostEnvironment)))
            .Returns(_environmentMock.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProviderMock.Object
            }
        };
    }

    [Fact]
    public void GetHealth_ReturnsHealthyStatus()
    {
        // Act
        var result = _controller.GetHealth();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();

        var response = okResult.Value;
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDetailedHealth_ReturnsHealthyStatus()
    {
        // Act
        var result = await _controller.GetDetailedHealth();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReadiness_ReturnsReady()
    {
        // Act
        var result = await _controller.GetReadiness();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public void GetLiveness_ReturnsAlive()
    {
        // Act
        var result = _controller.GetLiveness();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public void GetHealth_IncludesVersionInfo()
    {
        // Act
        var result = _controller.GetHealth();

        // Assert
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;

        // Check that response has required properties
        var responseType = response!.GetType();
        responseType.GetProperty("Version").Should().NotBeNull();
        responseType.GetProperty("Environment").Should().NotBeNull();
        responseType.GetProperty("Status").Should().NotBeNull();
        responseType.GetProperty("Timestamp").Should().NotBeNull();
    }

    [Fact]
    public async Task GetDetailedHealth_IncludesComponents()
    {
        // Act
        var result = await _controller.GetDetailedHealth();

        // Assert
        var okResult = result as OkObjectResult;
        var response = okResult!.Value;

        var responseType = response!.GetType();
        var componentsProperty = responseType.GetProperty("Components");
        componentsProperty.Should().NotBeNull();
    }
}
