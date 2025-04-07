namespace Infrastructure.Interfaces;

public interface ISignatureService
{
    public bool IsSignatureValid(string signatureBase64, string publicKeyPem, string dataBase64);
}