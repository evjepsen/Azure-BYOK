using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

public class SignatureService : ISignatureService
{
    private readonly ICertificateCache _certificateCache;
    private readonly ILogger<SignatureService> _logger;

    public SignatureService(ICertificateCache certificateCache, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SignatureService>();
        _certificateCache = certificateCache;
    }

    public bool IsSignatureValid(string signatureBase64, string dataBase64)
    {
        _logger.LogInformation("Verifying signature...");
        var certificate = _certificateCache.GetCertificate();

        // Check that a certificate is available
        if (certificate == null)
        {
            _logger.LogWarning("There was no certificate available to verify the signature in the cache.");
            throw new InvalidOperationException("No certificate was found.");
        }
        
        // Decode the base64 strings
        var signature = Convert.FromBase64String(signatureBase64);
        var data = Convert.FromBase64String(dataBase64);
        
        // Create a new RSA object from the certificate
        using var rsa = certificate.GetRSAPublicKey();

        if (rsa == null)
        {
            _logger.LogWarning("There was no RSA public key available to verify the signature in the certificate.");
            throw new InvalidOperationException("No RSA public key was found.");
        }
        
        return rsa.VerifyData(data, signature, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
    }
}