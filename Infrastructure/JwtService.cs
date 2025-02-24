using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Infrastructure.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Infrastructure;

public class JwtService : IJwtService
{
    public SecurityToken CreateToken()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        byte[] key = new byte[32];
        
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new (ClaimTypes.Name, "John Doe"),
                new (ClaimTypes.Role, "Admin"),
            }),
            Audiences = {"api://AzureADTokenExchange"},
            Issuer = "Jonas Iversen and Sons",
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        return tokenHandler.CreateToken(tokenDescriptor);
    }

    public string ConvertTokenToString(SecurityToken token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }
}