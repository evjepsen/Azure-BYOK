using Azure.Security.KeyVault.Keys;

namespace Infrastructure.Models;

/// <summary>
/// Signed response from the Key Vault of the KEK
/// </summary>
public class KekSignedResponse
{
    public required KeyVaultKey Kek { get; init; }
    public required string PemString { get; init; }
    public required string Base64EncodedSignature { get; init; }
}