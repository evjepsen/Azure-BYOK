using API;
using API.Controllers;
using Azure;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Test.TestHelpers;

namespace Test.Controllers;


[TestFixture]
[TestOf(typeof(AuditController))]
public class TestAuditController 
{
    private AuditController _auditController;
    private Mock<ILogger<AuditController>> _mockLogger;
    private Mock<IKeyVaultManagementService> _mockKeyVaultManagementService;
    private Mock<IAuditService> _mockAuditService;
    
    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<AuditController>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(logger => logger.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _mockKeyVaultManagementService = new Mock<IKeyVaultManagementService>(); 
        _mockAuditService = new Mock<IAuditService>();

        _auditController = new AuditController(_mockAuditService.Object, mockLoggerFactory.Object, _mockKeyVaultManagementService.Object );
    }

    [Test]
    public async Task ShouldValidKeyOperationsLogRequestReturnOk()
    {
        // Given an audit controller
        _mockAuditService.Setup(mock => mock.GetKeyOperationsPerformedAsync(It.IsAny<int>()))
            .ReturnsAsync("Valid JSON response");
        
        // When I ask for key operations performed
        const int numOfDays = 7;
        var result = await _auditController.GetKeyOperationsPerformed(numOfDays);
        // Then I should get a 200 OK response
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Getting key operations performed in the last {numOfDays} days");    
    }
    
    [Test]
    public async Task ShouldKeyOperationsLogRequestFailWhenLogsCannotBeGottenFromAzure()
    {
        // Given an audit controller
        const int status = StatusCodes.Status400BadRequest;
        _mockAuditService.Setup(mock => mock.GetKeyOperationsPerformedAsync(It.IsAny<int>()))
            .ThrowsAsync(new RequestFailedException(status, "Error"));
        
        // When I ask for key operations performed
        const int numOfDays = 7;
        var result = await _auditController.GetKeyOperationsPerformed(numOfDays);
        
        // Then I should get a 200 OK response
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        var objectResult = (ObjectResult) result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(status));
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Getting key operations performed in the last {numOfDays} days");  
        
        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger,
            LogLevel.Error,
            "Azure failed to get the logs:");  
    }
    
    [Test]
    public async Task ShouldKeyOperationsLogRequestFailWhenInternalServerErrorOccurs()
    {
        // Given an audit controller
        _mockAuditService.Setup(mock => mock.GetKeyOperationsPerformedAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception( "Error"));
        
        // When I ask for key operations performed
        const int numOfDays = 7;
        var result = await _auditController.GetKeyOperationsPerformed(numOfDays);
        
        // Then I should get a 200 OK response
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        var objectResult = (ObjectResult) result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Getting key operations performed in the last {numOfDays} days");  
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Error,
            Constants.InternalServerErrorOccuredGettingTheLogs);  
    }
    
    [Test]
    public async Task ShouldValidKeyVaultOperationsLogRequestReturnOk()
    {
        // Given an audit controller
        _mockAuditService.Setup(mock => mock.GetVaultOperationsPerformedAsync(It.IsAny<int>()))
            .ReturnsAsync("Valid JSON response");
        
        // When I ask for key operations performed
        const int numOfDays = 7;
        var result = await _auditController.GetVaultOperationsPerformed(numOfDays);
        // Then I should get a 200 OK response
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Getting vault operations performed in the last {numOfDays} days");    
    }
    
    [Test]
    public async Task ShouldValidKeyVaultActivityLogRequestReturnOk()
    {
        // Given an audit controller
        _mockAuditService.Setup(mock => mock.GetKeyVaultActivityLogsAsync(It.IsAny<int>()))
            .ReturnsAsync("Valid JSON response");
        
        // When I ask for key operations performed
        const int numOfDays = 7;
        var result = await _auditController.GetKeyVaultActivityLogs(numOfDays);
        
        // Then I should get a 200 OK response
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Getting vault activity logs from the last {numOfDays} days");    
    }

    [Test]
    public async Task ShouldSuccessfulGetRoleAssignmentsReturnOk()
    {
        // Given an audit controller
        _mockKeyVaultManagementService.Setup(mock => mock.GetRoleAssignmentsAsync())
            .ReturnsAsync([]);
        
        // When I ask for the role assignments
        var result = await _auditController.GetRoleAssignments();
        // Then I should get a 200 OK response
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            "Getting role assignments");
    }
    
    [Test]
    public async Task ShouldGetRoleAssignmentsReturnBadRequestWhenAzureFails()
    {
        // Given an audit controller
        const int status = StatusCodes.Status400BadRequest;
        _mockKeyVaultManagementService.Setup(mock => mock.GetRoleAssignmentsAsync())
            .ThrowsAsync(new RequestFailedException(status, "Error"));
        
        // When I ask for the role assignments
        var result = await _auditController.GetRoleAssignments();
        // Then I should get a 200 OK response
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        var objectResult = (ObjectResult) result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(status));
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            "Getting role assignments");
        
        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger,
            LogLevel.Error,
            "Azure failed to get the role assignments");
    }
        
    [Test]
    public async Task ShouldGetRoleAssignmentsReturnBadRequestWhenInternalServerErrorOccurs()
    {
        // Given an audit controller
        _mockKeyVaultManagementService.Setup(mock => mock.GetRoleAssignmentsAsync())
            .ThrowsAsync(new Exception( "Error"));
        
        // When I ask for the role assignments
        var result = await _auditController.GetRoleAssignments();
        
        // Then I should get a 200 OK response
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        var objectResult = (ObjectResult) result;
        Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            "Getting role assignments");
        
        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger,
            LogLevel.Error,
            Constants.InternalServerErrorOccuredWhenGettingRoleAssignments);
    }

        
    [TearDown]
    public void TearDownController()
    {
        _auditController.Dispose();
    }
}