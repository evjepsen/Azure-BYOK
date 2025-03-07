using Infrastructure.Models;

namespace Infrastructure.Interfaces;

public interface ITokenService
{
    public KeyTransferBlob CreateKeyTransferBlob(byte[] cipherText, string kekId);

    public string CreateBodyForRequest(KeyTransferBlob transferBlob);

}