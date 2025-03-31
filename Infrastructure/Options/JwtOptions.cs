using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Options;

public class JwtOptions
{
    public const string Jwt = "Jwt";
    
    [Required(ErrorMessage = "Issuer is required")]
    public string Issuer { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Audience is required")]
    public string Audience { get; set; } = string.Empty;
    
    [MinLength(64)]
    [Required(ErrorMessage = "Secret is required")]
    public string Secret { get; set; } = string.Empty;
}