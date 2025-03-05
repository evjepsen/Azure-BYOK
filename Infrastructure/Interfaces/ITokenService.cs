namespace Infrastructure.Interfaces;

public interface ITokenService
{
    public string CreateKeyTransferBlob(byte[] cipherText, string kekId);

    public string CreateBodyForRequest(string transferBlob);

}