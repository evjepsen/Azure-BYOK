using Azure.Security.KeyVault.Keys.Cryptography;

namespace Infrastructure.Interfaces;

/// <summary>
/// Service to sign and verify data using a certificate in the Azure Key Vault
/// </summary>
public interface ICertificateService
{
    /// <summary>
    /// Signs data using a certificate in the Azure Key Vault
    /// </summary>
    /// <param name="dataToSign">Data to sign</param>
    /// <returns>Result of sign operation</returns>
    public Task<SignResult> SignAsync(byte[] dataToSign);

    /// <summary>
    /// Verifies a signature using a certificate in the Azure Key Vault
    /// </summary>
    /// <param name="dataToVerify">Data to verify</param>
    /// <param name="signature">Signature of signed data</param>
    /// <returns>Result of verify operation</returns>
    public Task<VerifyResult> VerifyAsync(byte[] dataToVerify, byte[] signature);
}