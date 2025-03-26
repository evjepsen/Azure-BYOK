using Azure.Security.KeyVault.Keys;

namespace FakeHSM.Interfaces;

/// <summary>
/// A Fake HSM interface, that serves to "simulate" a HSM.
/// </summary>
public interface IFakeHsm
{
    /// <summary>
    /// Generate a Blob using a KeyVaultKey acting as a KEK
    /// </summary>
    /// <param name="kek">The Key Encryption Key</param>
    /// <returns>A byte array of the blob</returns>
    byte[] GenerateBlob(KeyVaultKey kek);
}