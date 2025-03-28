using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Options;

public class ApplicationOptions
{
    public const string Application = "ApplicationOptions";
    
    [Required(ErrorMessage = "Vault URI is required")]
    public string VaultUri { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subscription ID is required")]
    public string SubscriptionId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Resource Group Name is required")]
    public string ResourceGroupName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Key Vault Resource Name is required")]
    public string KeyVaultResourceName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Allowed emails is required")]
    [MinLength(1)]
    public List<string> AllowedEmails { get; set; } = new(); 
}