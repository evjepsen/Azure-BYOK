using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Options;

public class JwtOptions
{
    public const string Jwt = "Jwt";
    
    public string Issuer { get; set; } = string.Empty;
    
    public string Audience { get; set; } = string.Empty;
    
    [MinLength(64)]
    public string Secret { get; set; } = string.Empty;
}