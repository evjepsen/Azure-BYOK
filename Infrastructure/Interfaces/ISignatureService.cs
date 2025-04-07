namespace Infrastructure.Interfaces;

public interface ISignatureService
{
    public bool IsSignatureValid(string signatureBase64, string dataBase64);
}