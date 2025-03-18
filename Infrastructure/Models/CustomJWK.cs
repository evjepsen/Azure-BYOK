namespace Infrastructure.Models;

public class CustomJwk
{
    public string? Kid { get; set; }
    public string? Kty { get; init; }
    public string[]? KeyOps { get; init; }
    public string? KeyHsm { get; init; }
    public string? N { get; set; }
    public string? E { get; set; }
}