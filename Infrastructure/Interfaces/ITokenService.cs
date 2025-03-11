using Infrastructure.Models;

namespace Infrastructure.Interfaces;

public interface ITokenService
{
    public KeyTransferBlob CreateKeyTransferBlob(byte[] cipherText, string kekId);

    public UploadKeyRequestBody CreateBodyForRequest(KeyTransferBlob transferBlob);
    
    public string SerializeJsonObject<T>(T jsonObject);

}