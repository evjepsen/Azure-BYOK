using Azure.Security.KeyVault.Keys.Cryptography;

namespace Infrastructure.Interfaces;

public interface ICertificateService
{
    public Task<SignResult> SignKeyWithCertificateAsync(byte[] dataToSign);

    public Task<VerifyResult> VerifyCertificateAsync(byte[] digestToVerify, byte[] signature);
}