using API.Controllers;
using Azure;
using Azure.ResourceManager.Monitor;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Is = NUnit.Framework.Is;

namespace Test.Controllers;

[TestFixture]
[TestOf(typeof(AlertController))]

public class TestAlertController
{
    private AlertController _alertController;
    private Mock<IAlertService> _mockAlertService;
    private Mock<ILogger<AlertController>> _mockLogger;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<AlertController>>();
        _mockAlertService = new Mock<IAlertService>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(logger => logger.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);
        
        _alertController = new AlertController(_mockAlertService.Object,mockLoggerFactory.Object);
    }

    [Test]
    public async Task ShouldValidActionGroupsReturnOk()
    {
        // Given an alert controller and a valid action group
        var expectedResult = Mock.Of<ActionGroupResource>();
        _mockAlertService.Setup(mock => mock.GetActionGroupAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedResult);
            
        // When I ask to get the action group
        var actionGroupName = "test";
        var actionGroup = await _alertController.GetActionGroup(actionGroupName);
        
        // Then it should return an Ok result
        Assert.That(actionGroup, Is.Not.Null);
        Assert.That(actionGroup, Is.InstanceOf<OkObjectResult>());
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals($"Getting action group {actionGroupName}", o.ToString(), StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ShouldAzureErrorReturnRequestFailedExceptionWithErrorCode()
    {
        // Given an alert controller and an invalid action group
        const string groupName = "test-group";
        const int statusCode = StatusCodes.Status404NotFound;
        const string errorCode = "ResourceNotFound";
        
        _mockAlertService.Setup(mock => mock.GetActionGroupAsync(It.IsAny<string>()))
            .ThrowsAsync(new RequestFailedException(statusCode, errorCode));
        
        // When I ask to get the action group
        var result = await _alertController.GetActionGroup(groupName);
        
        // Then it should return a NotFound result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(statusCode));
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Azure failed to get action group {groupName}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Test]
    public async Task ShouldUnexpectedErrorReturnInternalServerError()
    {
        // Given an alert controller and an invalid action group
        const string groupName = "test-group";
        
        _mockAlertService.Setup(mock => mock.GetActionGroupAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Unexpected error"));
        
        // When I ask to get the action group
        var result = await _alertController.GetActionGroup(groupName);
        
        // Then it should return an InternalServerError result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        // Safe cast to ObjectResult
        var statusCodeResult = (ObjectResult) result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"An unexpected error occured when getting action group {groupName}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TearDown]
    public void TearDownController()
    {
        _alertController.Dispose();
    }
}