namespace Infrastructure.Models;


public class PublicKeyKekPem
{
    public required Uri KekId { get; init; }
    public required string PemString { get; init; }
}

