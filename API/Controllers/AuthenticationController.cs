using System.Security.Claims;
using Google.Apis.Auth.AspNetCore3;
using Infrastructure.Interfaces;
using Infrastructure.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

/// <summary>
/// Controller for handling authentication
/// </summary>
[Route("[controller]")]
public class AuthenticationController : Controller
{
    private readonly Dictionary<string,string> _schemeMap;
    private readonly List<string> _validEmails;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthenticationController> _logger;

    /// <summary>
    /// Constructor for the authentication controller
    /// </summary>
    public AuthenticationController(
        IOptions<ApplicationOptions> applicationOptions, 
        IJwtService jwtService,
        ILoggerFactory loggerFactory)
    {
        _jwtService = jwtService;
        _logger = loggerFactory.CreateLogger<AuthenticationController>();
        // Map over the valid authentication schemes
        _schemeMap = new Dictionary<string, string>
        {
            { "Microsoft", "MicrosoftAuth" },
            { "Google", GoogleOpenIdConnectDefaults.AuthenticationScheme }
        };
        
        // Valid email addresses
        _validEmails = applicationOptions.Value.AllowedEmails;

    }
    
    /// <summary>
    /// Uses Identity Service Providers (ISP) to authenticate the user
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login(string provider = "Microsoft")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(Callback), null, new { provider }, Request.Scheme),
            Items = 
            {
                { "provider", provider }
            }
        };

        if (!_schemeMap.TryGetValue(provider, out var scheme))
        {
            _logger.LogWarning("The provider {provider} is not supported", provider);
            return BadRequest("The provider is not supported");
        }
        
        _logger.LogInformation("Issuing the user a challenge for the provider {provider}", provider);
        return Challenge(properties, scheme);
    }

    /// <summary>
    /// Callback for the authentication process
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string provider)
    {
        var authResult = await HttpContext.AuthenticateAsync(_schemeMap[provider]);
        
        if (!authResult.Succeeded)
        {
            _logger.LogError("Authentication failed ({provider})", provider);
            return BadRequest("Authentication failed");
        }
        
        // Check that the user's emails is in the allowed list
        var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);
        var userId = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = authResult.Principal.FindFirstValue(ClaimTypes.Name);
        
        if (email == null || !_validEmails.Contains(email)) 
        {
            _logger.LogInformation("User {email} tried to access the application", email);
            return Unauthorized("No access");
        }

        // Create the JWT Access token for further use of the API
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId ?? string.Empty),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name ?? string.Empty),
            new("provider", provider),
        };
        
        _logger.LogInformation("Generating the user {email} an access token", email);
        var accessToken = _jwtService.GenerateAccessToken(claims);
        
        // Return the access token for further use
        return Ok(new {accessToken});
    }
}