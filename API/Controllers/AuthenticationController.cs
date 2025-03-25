using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Controller for handling authentication
/// </summary>
[Route("[controller]")]
public class AuthenticationController : Controller
{
    private readonly Dictionary<string,string> _schemeMap;

    /// <summary>
    /// Constructor for the authentication controller
    /// </summary>
    public AuthenticationController()
    {
        // Map over the valid authentication schemes
        _schemeMap = new Dictionary<string, string>
        {
            { "Microsoft", OpenIdConnectDefaults.AuthenticationScheme },
            { "Google", GoogleDefaults.AuthenticationScheme }
        };
    }
    
    /// <summary>
    /// Uses Identity Service Providers (ISP) to authenticate the user
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login(string provider = "Microsoft")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(Callback))
        };

        if (!_schemeMap.TryGetValue(provider, out var scheme))
        {
            return BadRequest("The provider is not supported");
        }
        
        return Challenge(properties, scheme);
    }

    /// <summary>
    /// Callback for the authentication process
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
    {
        AuthenticateResult? authenticationResult = null;
        string? authenticatedScheme = null;
        
        // Try to authenticate with the different schemes
        foreach (var scheme in _schemeMap.Values)
        {
            authenticationResult = await HttpContext.AuthenticateAsync(scheme);
            if (!authenticationResult.Succeeded) continue;
            authenticatedScheme = scheme;
            break;
        }
        
        if (authenticationResult == null || !authenticationResult.Succeeded)
        {
            return Unauthorized("Authentication failed");
        }
        
        var accessToken = await HttpContext.GetTokenAsync(authenticatedScheme, "access_token");

        return Ok(new {accessToken});
    }
}