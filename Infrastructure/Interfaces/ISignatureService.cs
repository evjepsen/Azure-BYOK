using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys.Cryptography;

namespace Infrastructure.Interfaces;

/// <summary>
/// Service used to verify the customer's signature
/// </summary>
public interface ISignatureService
{
    /// <summary>
    /// Check whether the signature given is valid
    /// </summary>
    /// <param name="signatureBase64">The signature in base64</param>
    /// <param name="data">The signed data</param>
    /// <returns>True if the signature is valid</returns>
    public bool IsCustomerSignatureValid(string signatureBase64, byte[] data);

    /// <summary>
    /// Get the signed data using the key data and the timestamp
    /// </summary>
    /// <param name="keyData">The data on the key</param>
    /// <param name="timeStamp">The timestamp of sending the request</param>
    /// <returns>A byte array containing the signed data</returns>
    public byte[] GetCustomerUploadSignedData(byte[] keyData, DateTime timeStamp);

    /// <summary>
    /// Verifies a signature using a certificate in the Azure Key Vault
    /// </summary>
    /// <param name="dataToVerify">Data to verify</param>
    /// <param name="signature">Signature of signed data</param>
    /// <returns>Result of verify operation</returns>
    public Task<VerifyResult> UseAzureToVerify(byte[] dataToVerify, byte[] signature);
    
    /// <summary>
    /// Signs data using a certificate in the Azure Key Vault
    /// </summary>
    /// <param name="dataToSign">Data to sign</param>
    /// <returns>Result of sign operation</returns>
    public Task<SignResult> UseAzureToSign(byte[] dataToSign);

    /// <summary>
    /// Retries the azure signing certificate from the key vault
    /// </summary>
    /// <returns>The Azure signing certificate</returns>
    /// <exception cref="ResourceNotFound">If the request to the key vault has no value (probably does not exsist)</exception>
    public Task<KeyVaultCertificateWithPolicy> GetAzureSigningCertificate();

    /// <summary>
    /// Converts the KeyVaultCertificateWithPolicy to a PEM string
    /// </summary>
    /// <param name="azureCertificate"></param>
    /// <returns>String </returns>
    public string KeyVaultCertificateToX509PemString(KeyVaultCertificateWithPolicy azureCertificate);

}