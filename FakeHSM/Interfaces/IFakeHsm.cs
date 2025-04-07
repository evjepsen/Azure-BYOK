using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Keys;
using Infrastructure.Models;

namespace FakeHSM.Interfaces;

/// <summary>
/// A Fake HSM interface, that serves to "simulate" a HSM.
/// </summary>
public interface IFakeHsm
{
    /// <summary>
    /// Generate the "ciphertext" part of the Blob using a KeyVaultKey acting as a KEK
    /// </summary>
    /// <param name="kek">The key encryption key from the key vault</param>
    /// <returns>Ciphertext</returns>
    public string EncryptPrivateKeyForUpload(RSA kek);

    /// <summary>
    /// Generate a transfer blob for upload to the Azure Key Vault
    /// </summary>
    /// <param name="kek">The key encryption key from the key vault</param>
    /// <param name="kekId">Id of the kek used</param>
    /// <returns>The blob in json format</returns>
    public KeyTransferBlob GenerateBlobForUpload(RSA kek, string kekId);

    /// <summary>
    /// Sign the data using private key
    /// </summary>
    /// <param name="keyDataBase64">Data to sign</param>
    /// <param name="timeStamp">The timestamp of signing</param>
    /// <returns>The data signature in base64</returns>
    public string SignData(byte[] keyDataBase64, DateTime timeStamp);
    
    /// <summary>
    /// Gets a self-signed certificate for the private key
    /// </summary>
    /// <returns>The self-signed certificate in X509 format</returns>
    public X509Certificate2 GetCertificateForPrivateKey();
}