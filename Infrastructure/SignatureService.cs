using Infrastructure.Interfaces;

namespace Infrastructure;

public class SignatureService : ISignatureService
{
    public bool IsSignatureValid(string signatureBase64, string publicKeyPem, string dataBase64)
    {
        throw new NotImplementedException();
    }
}