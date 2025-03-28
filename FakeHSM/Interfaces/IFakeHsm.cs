using System.Security.Cryptography;
using Azure.Security.KeyVault.Keys;

namespace FakeHSM.Interfaces;

/// <summary>
/// A Fake HSM interface, that serves to "simulate" a HSM.
/// </summary>
public interface IFakeHsm
{
    /// <summary>
    /// Generate the "ciphertext" part of the Blob using a KeyVaultKey acting as a KEK
    /// </summary>
    /// <returns>Ciphertext</returns>
    byte[] GeneratePrivateKeyForBlob(RSA rsaKey);
}