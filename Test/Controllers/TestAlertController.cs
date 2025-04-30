using API.Controllers;
using Azure;
using Azure.ResourceManager.Monitor;
using Infrastructure.Interfaces;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Test.TestHelpers;
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
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger, 
            LogLevel.Information,
            $"Getting action group {actionGroupName}");
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
        
        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger, 
            LogLevel.Error,
            $"Azure failed to get action group {groupName}");
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
        
        MockLoggerTestHelper.VerifyLogContains(
                _mockLogger,
                LogLevel.Error,
                $"An unexpected error occured when getting action group {groupName}");
    }
    

    [Test]
    public async Task ShouldCreateActionGroupReturnOk()
    {
        // Given a valid action group name and receivers
        const string groupName = "test-group";
        var receivers = new List<EmailReceiver>
        {
            new() { Name = "John Doe", Email = "john.doe@example.com" }
        };
        var expectedResult = Mock.Of<ActionGroupResource>();

        _mockAlertService.Setup(mock => mock.CreateActionGroupAsync(groupName, receivers))
            .ReturnsAsync(expectedResult);

        // When I call CreateActionGroup
        var result = await _alertController.CreateActionGroup(groupName, receivers);

        // Then it should return an Ok result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());

        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Creating action group {groupName}");
    }
    
    [Test]
    public async Task ShouldCreateActionGroupReturnBadRequestWhenNoReciversArePassedIn()
    {
        // Given a valid action group name and receivers, but Azure throws a RequestFailedException
        const string groupName = "test-group";
        var receivers = new List<EmailReceiver>();

        // When I call CreateActionGroup
        var result = await _alertController.CreateActionGroup(groupName, receivers);

        // Then it should return the appropriate status code and error code
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());

        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger,
            LogLevel.Error,
            $"No receivers specified for action group {groupName}");
    }

    [Test]
    public async Task ShouldCreateActionGroupHandleRequestFailedException()
    {
        // Given a valid action group name and receivers, but Azure throws a RequestFailedException
        const string groupName = "test-group";
        var receivers = new List<EmailReceiver>
        {
            new () { Name = "John Doe", Email = "john.doe@example.com" }
        };
        const int statusCode = StatusCodes.Status400BadRequest;
        const string errorCode = "InvalidRequest";

        _mockAlertService.Setup(mock => mock.CreateActionGroupAsync(groupName, receivers))
            .ThrowsAsync(new RequestFailedException(statusCode, errorCode));

        // When I call CreateActionGroup
        var result = await _alertController.CreateActionGroup(groupName, receivers);

        // Then it should return the appropriate status code and error code
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());

        var statusCodeResult = (ObjectResult)result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(statusCode));

        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger,
            LogLevel.Error,
            $"Azure failed to create action group {groupName}");
    }

    [Test]
    public async Task ShouldCreateActionGroupHandleUnexpectedException()
    {
        // Given a valid action group name and receivers, but an unexpected exception occurs
        const string groupName = "test-group";
        var receivers = new List<EmailReceiver>
        {
            new () { Name = "John Doe", Email = "john.doe@example.com" }
        };

        _mockAlertService.Setup(mock => mock.CreateActionGroupAsync(groupName, receivers))
            .ThrowsAsync(new Exception("Unexpected error"));

        // When I call CreateActionGroup
        var result = await _alertController.CreateActionGroup(groupName, receivers);

        // Then it should return an InternalServerError result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());

        var statusCodeResult = (ObjectResult)result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));

        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger,
            LogLevel.Error,
            $"An unexpected error occured when creating action group {groupName}");
    }
    
    [Test]
    public async Task ShouldCreateKeyVaultAlertReturnOk()
    {
        // Given a valid alert name and action groups
        const string alertName = "test-alert";
        var actionGroups = new List<string> { "action-group-1", "action-group-2" };
        var expectedResult = Mock.Of<ActivityLogAlertResource>();

        _mockAlertService.Setup(mock => mock.CreateAlertForKeyVaultAsync(alertName, actionGroups))
            .ReturnsAsync(expectedResult);

        // When I call CreateKeyVaultAlert
        var result = await _alertController.CreateKeyVaultAlert(alertName, actionGroups);

        // Then it should return an Ok result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());

        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Creating key vault activity alert {alertName}");
    }

    [Test]
    public async Task ShouldCreateKeyVaultAlertReturnBadRequestWhenNoActionGroupsArePassedIn()
    {
        // Given a valid alert name but no action groups
        const string alertName = "test-alert";
        var actionGroups = new List<string>();

        // When I call CreateKeyVaultAlert
        var result = await _alertController.CreateKeyVaultAlert(alertName, actionGroups);

        // Then it should return a BadRequest result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());

        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger,
            LogLevel.Error,
            $"No action groups specified for alert {alertName}");
    }

    [Test]
    public async Task ShouldCreateKeyVaultAlertHandleRequestFailedException()
    {
        // Given a valid alert name and action groups, but Azure throws a RequestFailedException
        const string alertName = "test-alert";
        var actionGroups = new List<string> { "action-group-1", "action-group-2" };
        const int statusCode = StatusCodes.Status400BadRequest;
        const string errorCode = "InvalidRequest";

        _mockAlertService.Setup(mock => mock.CreateAlertForKeyVaultAsync(alertName, actionGroups))
            .ThrowsAsync(new RequestFailedException(statusCode, errorCode));

        // When I call CreateKeyVaultAlert
        var result = await _alertController.CreateKeyVaultAlert(alertName, actionGroups);

        // Then it should return the appropriate status code and error code
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());

        var statusCodeResult = (ObjectResult)result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(statusCode));

        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger,
            LogLevel.Error,
            $"Azure failed to create key vault alert {alertName}");
    }

    [Test]
    public async Task ShouldCreateKeyVaultAlertHandleUnexpectedException()
    {
        // Given a valid alert name and action groups, but an unexpected exception occurs
        const string alertName = "test-alert";
        var actionGroups = new List<string> { "action-group-1", "action-group-2" };

        _mockAlertService.Setup(mock => mock.CreateAlertForKeyVaultAsync(alertName, actionGroups))
            .ThrowsAsync(new Exception("Unexpected error"));

        // When I call CreateKeyVaultAlert
        var result = await _alertController.CreateKeyVaultAlert(alertName, actionGroups);

        // Then it should return an InternalServerError result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());

        var statusCodeResult = (ObjectResult)result;
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));

        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger,
            LogLevel.Error,
            $"An unexpected error occured when creating key vault alert {alertName}");
    }

    [TearDown]
    public void TearDownController()
    {
        _alertController.Dispose();
    }
}