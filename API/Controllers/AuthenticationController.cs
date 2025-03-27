using System.Security.Claims;
using Google.Apis.Auth.AspNetCore3;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.Configuration;

namespace API.Controllers;

/// <summary>
/// Controller for handling authentication
/// </summary>
[Route("[controller]")]
public class AuthenticationController : Controller
{
    private readonly Dictionary<string,string> _schemeMap;
    private readonly string[] _validEmails;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Constructor for the authentication controller
    /// </summary>
    public AuthenticationController(IConfiguration configuration, ITokenService tokenService)
    {
        _tokenService = tokenService;
        // Map over the valid authentication schemes
        _schemeMap = new Dictionary<string, string>
        {
            { "Microsoft", "MicrosoftAuth" },
            { "Google", GoogleOpenIdConnectDefaults.AuthenticationScheme }
        };
        
        // Valid email addresses
        _validEmails = configuration
            .GetSection("AllowedEmails")
            .Get<string []>() ?? throw new InvalidConfigurationException("No valid emails found in configuration");;
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
            return BadRequest("The provider is not supported");
        }
        
        if (provider == "Google")
        {
            properties.Items["prompt"] = "select_account";
            properties.Items["scope"] = "openid email profile";
        }
        
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
            return BadRequest("Authentication failed");
        }
        
        // Check that the user's emails is in the allowed list
        var email = authResult.Principal.FindFirstValue(ClaimTypes.Email);
        var userId = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = authResult.Principal.FindFirstValue(ClaimTypes.Name);
        
        if (!_validEmails.Contains(email)) 
        {
            return Unauthorized();
        }

        // Create the JWT Access token for further use of the API
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId ?? string.Empty),
            new(ClaimTypes.Email, email ?? string.Empty),
            new(ClaimTypes.Name, name ?? string.Empty),
            new("provider", provider),
        };
        
        var accessToken = _tokenService.GenerateAccessToken(claims);
        
        // Return the access token for further use
        return Ok(new {accessToken});
    }
}