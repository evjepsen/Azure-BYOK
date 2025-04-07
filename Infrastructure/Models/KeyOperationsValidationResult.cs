namespace Infrastructure.Models;

public class KeyOperationsValidationResult
{
    public required bool IsValid { get; set; }
    
    public required string ErrorMessage { get; set; }
}