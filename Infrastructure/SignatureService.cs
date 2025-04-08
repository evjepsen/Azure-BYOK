using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys.Cryptography;
using Infrastructure.Interfaces;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public class SignatureService : ISignatureService
{
    private readonly ICertificateCache _certificateCache;
    private readonly ILogger<SignatureService> _logger;
    private readonly CertificateClient _client;
    private readonly ApplicationOptions _applicationOptions;
    private readonly ICryptographyClientFactory _cryptographyClientFactory;

    public SignatureService(ICertificateCache certificateCache, 
        IHttpClientFactory httpClientFactory, 
        IOptions<ApplicationOptions> applicationOptions,
        ICryptographyClientFactory cryptographyClientFactory,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SignatureService>();
        _certificateCache = certificateCache;
        _cryptographyClientFactory = cryptographyClientFactory;
        _applicationOptions = applicationOptions.Value;
        
        var tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions());
        _client = new CertificateClient(
            new Uri(_applicationOptions.VaultUri),
            tokenCredential,
            new CertificateClientOptions
            {
                Transport = new HttpClientTransport(httpClientFactory.CreateClient("WaitAndRetry"))
            }
        );
    }

    public bool IsCustomerSignatureValid(string signatureBase64, byte[] data)
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
        
        // Create a new RSA object from the certificate
        using var rsa = certificate.GetRSAPublicKey();

        if (rsa == null)
        {
            _logger.LogWarning("There was no RSA public key available to verify the signature in the certificate.");
            throw new InvalidOperationException("No RSA public key was found.");
        }
        
        return rsa.VerifyData(data, signature, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
    }

    public byte[] GetCustomerUploadSignedData(byte[] keyData, DateTime timeStamp)
    {
        _logger.LogInformation("Setting up the data to verify the signature on");
        
        var timeStampAsString = timeStamp.ToString(CultureInfo.CurrentCulture);
        var timestampData = System.Text.Encoding.UTF8.GetBytes(timeStampAsString);
        
        return keyData.Concat(timestampData).ToArray();
    }

    public async Task<VerifyResult> UseAzureToVerify(byte[] dataToVerify, byte[] signature)
    {
        // Create a CryptographyClient using the ID of the certificate
        var certificate = await GetAzureSigningCertificate();
        var cryptoClient = _cryptographyClientFactory.GetCryptographyClient(certificate.KeyId);
        
        // Prepare data for signing, should be pre-hashed
        var digestToVerify = DigestData(dataToVerify);

        // Verify the signature using the CryptographyClient
        var verifyResult = await cryptoClient.VerifyAsync(SignatureAlgorithm.RS256, digestToVerify,signature);

        return verifyResult;
        
    }

    public async Task<SignResult> UseAzureToSign(byte[] dataToSign)
    {
        try
        {
            // Create a CryptographyClient using the ID of the certificate
            var certificate = await GetAzureSigningCertificate();
            var cryptoClient = _cryptographyClientFactory.GetCryptographyClient(certificate.KeyId);
            
            // Prepare data for signing, should be pre-hashed
            var hash = DigestData(dataToSign);
            
            // Sign the data using the CryptographyClient
            _logger.LogInformation("Key vault signed kek ({length} bytes)", dataToSign.Length);
            var signResult = await cryptoClient.SignAsync(SignatureAlgorithm.RS256, hash);
            
            return signResult;
        }
        catch (Exception e)
        {
            // Catch in order to log the error
            _logger.LogError("Could not sign data. Failed with error: {error}", e.Message);
            // Rethrow the exception to propagate exception to controller
            throw;
        }
         
    }

    public async Task<KeyVaultCertificateWithPolicy> GetAzureSigningCertificate()
    {
        var certWithPolicy = await _client.GetCertificateAsync(_applicationOptions.SigningCertificateName);

        if (!certWithPolicy.HasValue)
        {
            throw new InvalidOperationException("No certificate was found.");
        }
        
        return certWithPolicy.Value;
    }

    // Helper method to get the digest
    private static byte[] DigestData(byte[] data) => SHA256.HashData(data);
}