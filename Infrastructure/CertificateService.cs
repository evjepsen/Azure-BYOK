using System.Security.Cryptography;
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

public class CertificateService : ICertificateService
{
    private readonly ApplicationOptions _applicationOptions;
    private readonly ILogger<CertificateService> _logger;
    private readonly CertificateClient _client;
    private readonly TokenCredential _tokenCredential;

    public CertificateService(
        IHttpClientFactory httpClientFactory, 
        IOptions<ApplicationOptions> applicationOptions,
        ILoggerFactory loggerFactory
        
        )
    {
        _applicationOptions = applicationOptions.Value;
        _logger = loggerFactory.CreateLogger<CertificateService>();
        _tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions());
        _applicationOptions = applicationOptions.Value;
        _client = new CertificateClient(
            new Uri(_applicationOptions.VaultUri),
            _tokenCredential,
            new CertificateClientOptions() 
            {
                Transport = new HttpClientTransport(httpClientFactory.CreateClient("WaitAndRetry"))
            }
        );
    }

    private static byte[] DigestData(byte[] data)
    {
        var digest = SHA256.HashData(data);
        
        return digest;
    }
    public async Task<SignResult> SignAsync(byte[] dataToSign)
    {
        try
        {
            // Get the certificate
            var certWithPolicy = await _client.GetCertificateAsync(_applicationOptions.SigningCertificateName);
            var certId = certWithPolicy.Value.KeyId;

            // Create a CryptographyClient using the ID of the certificate
            var cryptoClient = new CryptographyClient(certId, _tokenCredential);
            
            // Prepare data for signing, should be pre-hashed
            var hash = DigestData(dataToSign);
            
            // Sign the data using the CryptographyClient
            var signResult = await cryptoClient.SignAsync(SignatureAlgorithm.RS256, hash);
            _logger.LogInformation("Key vault signed {length} bytes", dataToSign.Length);
            
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

    public async Task<VerifyResult> VerifyAsync(byte[] dataToVerify, byte[] signature)
    {
        // Get the certificate
        var certWithPolicy = await _client.GetCertificateAsync(_applicationOptions.SigningCertificateName);
        var certId = certWithPolicy.Value.KeyId;

        // Create a CryptographyClient using the ID of the certificate
        var cryptoClient = new CryptographyClient(certId, _tokenCredential);
        
        // Prepare data for signing, should be pre-hashed
        var digestToVerify = DigestData(dataToVerify);

        // Verify the signature using the CryptographyClient
        var verifyResult = await cryptoClient.VerifyAsync(SignatureAlgorithm.RS256, digestToVerify,signature);

        return verifyResult;
    }

}