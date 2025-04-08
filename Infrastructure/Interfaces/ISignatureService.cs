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
    public byte[] GetSignedData(byte[] keyData, DateTime timeStamp);
}