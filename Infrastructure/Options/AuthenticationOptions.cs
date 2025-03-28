using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Options;

public class AuthenticationOptions
{
    public const string Microsoft = "Authentication:Microsoft";
    public const string Google = "Authentication:Google";
    
    [Required(ErrorMessage = "ClientId is required")]
    public string ClientId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "ClientSecret is required")]
    public string ClientSecret { get; set; } = string.Empty;
}