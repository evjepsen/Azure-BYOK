using System.Security.Claims;

namespace Infrastructure.Interfaces;

/// <summary>
/// Service used to create and handle JWT access tokens
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Creates an access token to be used for subsequent API requests
    /// </summary>
    /// <param name="claims">The list of claims to include in the access token</param>
    /// <returns>The JWT access token to be used in API requests</returns>
    public string GenerateAccessToken(List<Claim> claims);
}