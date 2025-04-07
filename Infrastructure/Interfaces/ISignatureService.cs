namespace Infrastructure.Interfaces;

public interface ISignatureService
{
    public bool IsSignatureValid(string signatureBase64, byte[] data);

    public byte[] GetSignedData(byte[] keyData, DateTime timeStamp);
}