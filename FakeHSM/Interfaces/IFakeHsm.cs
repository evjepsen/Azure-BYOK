using Azure.Security.KeyVault.Keys;

namespace FakeHSM.Interfaces;

/// <summary>
/// A Fake HSM interface, that serves to "simulate" a HSM.
/// </summary>
public interface IFakeHsm
{
    /// <summary>
    /// Generate a RSA key pair
    /// </summary>
    /// <param name="bitLength"> The bit-length of the key</param>
    /// <returns>a tuple of (1) id of the keyEntry associated with the key and (2) public key as a bytearray</returns>
    (string randomId, byte[] pk) GenerateRsaKey(int bitLength);
    /// <summary>
    /// Generate an AES key
    /// </summary>
    /// <param name="bitLength"></param>
    /// <returns>Id of the keyEntry of the key</returns>
    string GenerateAesKey(int bitLength);
    /// <summary>
    /// Get the public key of a key with a given id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Public key in the "X.509 SubjectPublicKeyInfo format"</returns>
    /// <exception cref="KeyNotFoundException">`id` is not associated with a key</exception>
    byte[]? GetPublicKeyOfId(string id);
    /// <summary>
    /// Gets a list of id of the keys in the HSM
    /// </summary>
    /// <returns>List of ids of the keys in the HSM</returns>
    /// <exception cref="KeyNotFoundException">`id` is not associated with a key</exception>
    List<string> GetKeyIds();
    /// <summary>
    /// Get the key properties of a key with a given id
    /// </summary>
    /// <param name="id"></param>
    /// <returns>KeyProperties for the given key</returns>
    /// <exception cref="KeyNotFoundException">`id` is not associated with a key</exception>
    KeyProperties GetKeyProperties(string id);
    /// <summary>
    /// Encrypt data with a key with a given id
    /// Interacts with the HSM to get the key object and encrypt the data
    /// </summary>
    /// <param name="id">id of the key</param>
    /// <param name="data">Plaintext: Data to be encrypted</param>
    /// <returns>Ciphertext</returns>
    /// <exception cref="KeyNotFoundException">`id` is not associated with a key</exception>
    byte[] EncryptWithKey(string id, byte[] data);
    /// <summary>
    /// Generate a Blob using a KeyVaultKey acting as a KEK
    /// </summary>
    /// <param name="kek">The Key Encryption Key</param>
    /// <returns>A byte array of the blob</returns>
    byte[] GenerateBlob(KeyVaultKey kek);
}