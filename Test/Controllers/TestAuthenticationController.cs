using API.Controllers;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Test.TestHelpers;

namespace Test.Controllers;

[TestFixture]
[TestOf(typeof(AuthenticationController))]
public class TestAuthenticationController
{
    private Mock<IJwtService> _mockJwtService;
    private Mock<ILogger<AuthenticationController>> _mockLogger;
    private AuthenticationController _authenticationController;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<AuthenticationController>>();
        _mockJwtService = new Mock<IJwtService>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(logger => logger.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        
        var configuration = TestHelper.CreateTestConfiguration();
        var applicationOptions = TestHelper.CreateApplicationOptions(configuration);
        _authenticationController = new AuthenticationController(applicationOptions, _mockJwtService.Object, mockLoggerFactory.Object);
        SetupControllerContext();
    }

    private void SetupControllerContext()
    {
        // Create mock HttpContext
        var httpContext = new DefaultHttpContext();
        
        // Set up URL helper
        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper
            .Setup(m => m.Action(It.IsAny<UrlActionContext>()))
            .Returns("https://test.com/callback");
        
        // Set request scheme
        httpContext.Request.Scheme = "https";
        
        // Set up controller context
        _authenticationController.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        // Assign URL helper to controller
        _authenticationController.Url = mockUrlHelper.Object;
    }


    [Test]
    public void ShouldChallengeBeIssuedForMicrosoft()
    {
        // Given an authentication controller
        
        // When I ask to log in with Microsoft
        // Set up a default http context that the authentication controller has access to
        
        var result = _authenticationController.Login();
        // Then it should return a challenge result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ChallengeResult>());
    }
    
    


    [TearDown]
    public void TearDownController()
    {
        _authenticationController.Dispose();
    }
}