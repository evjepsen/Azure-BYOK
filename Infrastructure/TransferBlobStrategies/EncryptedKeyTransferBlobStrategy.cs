using Infrastructure.Interfaces;
using Infrastructure.Models;

namespace Infrastructure.TransferBlobStrategies;

public class EncryptedKeyTransferBlobStrategy : ITransferBlobStrategy
{
    private readonly string _kekId;
    private readonly string _encryptedKey;
    private readonly ITokenService _tokenService;

    public EncryptedKeyTransferBlobStrategy(string kekId, string encryptedKey, ITokenService tokenService)
    {
        _kekId = kekId;
        _encryptedKey = encryptedKey;
        _tokenService = tokenService;
    }

    public KeyTransferBlob GenerateTransferBlob()
    {
        // Extract encrypted key
        var encryptedKey = Convert.FromBase64String(_encryptedKey);
        // Create the BYOK Blob for upload
        return _tokenService.CreateKeyTransferBlob(encryptedKey, _kekId);    
    }
}