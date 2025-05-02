using System.Security.Claims;
using API.Controllers;
using API.Models;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authentication;
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
    private Mock<IAuthenticationService> _mockAuthService;

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
        
        // Add a valid email to the list of allowed emails for testing
        applicationOptions.Value.AllowedEmails.Add("valid@example.com");

        _mockJwtService.Setup(mock => mock.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns("token");
        
        _authenticationController = new AuthenticationController(applicationOptions, _mockJwtService.Object, mockLoggerFactory.Object);
        SetupControllerContext();
    }

    private void SetupControllerContext()
    {
        _mockAuthService = new Mock<IAuthenticationService>();

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(s => s.GetService(typeof(IAuthenticationService)))
            .Returns(_mockAuthService.Object);
        
        // Create mock HttpContext
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider.Object
        };
        
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
        const string provider = "Microsoft";
        var result = _authenticationController.Login(provider);
        // Then it should return a challenge result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ChallengeResult>());
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Issuing the user a {provider} provided challenge");
    }
    
    [Test]
    public void ShouldChallengeNotBeIssuedForUnknownProvider()
    {
        // Given an authentication controller
        
        // When I ask to log in with GitHub
        const string provider = "github";
        var result = _authenticationController.Login(provider);

        // Then it should return a bad result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Warning,
            $"The provider {provider} is not supported");
    }

    [Test]
    public async Task ShouldCallbackReturnErrorForUnknownProvider()
    {
        // Given an authentication controller
        // When I call the callback with the provider GitHub
        const string provider = "github";
        var result = await _authenticationController.Callback(provider);
        
        // Then it should return a bad result
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Warning,
            $"The provider {provider} is not supported");
    }

    [Test]
    public async Task ShouldCallBackReturnAccessTokenOnAuthorizedUser()
    {
        // Given an authentication controller
        const string email = "valid@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.NameIdentifier, "user123"),
            new(ClaimTypes.Name, "Test User")
        };
        
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        
        var authResult = AuthenticateResult.Success(
            new AuthenticationTicket(principal, "MicrosoftAuth"));
        
        _mockAuthService
            .Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), "MicrosoftAuth"))
            .ReturnsAsync(authResult);
        
        
        // When I call the callback with the provider Microsoft and a valid user
        var res = await _authenticationController.Callback("Microsoft");
        
        // Then it should return an access token
        Assert.That(res, Is.Not.Null);
        Assert.That(res, Is.InstanceOf<OkObjectResult>());
        
        var okResult = (OkObjectResult) res;
        
        Assert.That(okResult.Value, Is.Not.Null);
        Assert.That(okResult.Value, Is.InstanceOf<CallbackResult>());
        
        var callbackResult = (CallbackResult) okResult.Value;
        Assert.That(callbackResult.AccessToken, Is.EqualTo("token"));
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"Generating the user {email} an access token");
    }
    
    [Test]
    public async Task ShouldCallBackReturnUnauthorizedOnUnAuthorizedUser()
    {
        // Given an authentication controller
        const string email = "invalid@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.NameIdentifier, "user123"),
            new(ClaimTypes.Name, "Test User")
        };
        
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        
        var authResult = AuthenticateResult.Success(
            new AuthenticationTicket(principal, "MicrosoftAuth"));
        
        _mockAuthService
            .Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), "MicrosoftAuth"))
            .ReturnsAsync(authResult);
        
        
        // When I call the callback with the provider Microsoft and a valid user
        var res = await _authenticationController.Callback("Microsoft");
        
        // Then it should return an access token
        Assert.That(res, Is.Not.Null);
        Assert.That(res, Is.InstanceOf<UnauthorizedObjectResult >());
        
        
        MockLoggerTestHelper.VerifyLogEntry(
            _mockLogger,
            LogLevel.Information,
            $"User {email} tried to access the application");
    }
    
    [Test]
    public async Task ShouldCallBackReturnUnauthorizedOnFailedAuthentication()
    {
        // Given an authentication controller
        var authResult = AuthenticateResult.NoResult();
        
        _mockAuthService
            .Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), "MicrosoftAuth"))
            .ReturnsAsync(authResult);
        
        
        // When I call the callback with the provider Microsoft and a valid user
        var res = await _authenticationController.Callback("Microsoft");
        
        // Then it should return an access token
        Assert.That(res, Is.Not.Null);
        Assert.That(res, Is.InstanceOf<UnauthorizedObjectResult>());
        
        MockLoggerTestHelper.VerifyLogContains(
            _mockLogger,
            LogLevel.Error,
            "Authentication failed (Microsoft)");
    }

    [TearDown]
    public void TearDownController()
    {
        _authenticationController.Dispose();
    }
}