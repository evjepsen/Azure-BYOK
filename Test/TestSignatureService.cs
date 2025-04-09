using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Infrastructure;
using Infrastructure.Factories;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Test.TestHelpers;

namespace Test;

[TestFixture]
[TestOf(typeof(SignatureService))]
public class TestSignatureService
{
    private ISignatureService _signatureService;
    private ICertificateCache _certificateCache;
    private IKeyVaultService _keyVaultService;

    [SetUp]
    public void Setup()
    {
        var configuration = TestHelper.CreateTestConfiguration();
        var applicationOptions = TestHelper.CreateApplicationOptions(configuration);
        var tokenService = new TokenService(new NullLoggerFactory());
        var httpClientFactory = new FakeHttpClientFactory();
        ICryptographyClientFactory cryptographyClientFactory = new CryptographyClientFactory(httpClientFactory);
        
        // Initialize the certificate cache and signature service
        _certificateCache = new CertificateCache(new NullLoggerFactory(), applicationOptions);
        _signatureService = new SignatureService(_certificateCache, 
            httpClientFactory, 
            applicationOptions, 
            cryptographyClientFactory,
            new NullLoggerFactory());
        _keyVaultService = new KeyVaultService(tokenService, 
            _signatureService,
            httpClientFactory, 
            applicationOptions, 
            new NullLoggerFactory());
    }
    
    [Test]
    public void ShouldThrowExceptionWhenCertificateNotFound()
    {
        var data = "Test data"u8.ToArray();
        // Given a signature service 
        // When I ask check whether a signature is valid
        var exception = Assert.Throws<InvalidOperationException>(() => _signatureService.IsCustomerSignatureValid("Test signature", data));

        // Then it should throw an exception
        Assert.That(exception.Message, Is.EqualTo("No certificate was found."));
    }
    
    [Test]
    public void ShouldBePossibleToVerifySignature()
    {
        // Given a signature service and a certificate stored in the cache (Which has been used to sign a message)
        var key = RSA.Create();
        var certificate = TestHelper.CreateCertificate(
            key, 
            "cn=Customer HSM",
            DateTimeOffset.Now,
            DateTimeOffset.Now.AddYears(1)
        );
        _certificateCache.AddCertificate(certificate);
        
        // When I ask to verify the signature
        var (signature, data) = TestHelper.CreateSignature(key);
        var isValid = _signatureService.IsCustomerSignatureValid(signature, data);
        
        // Then it should be successful
        Assert.That(isValid, Is.True);
    }
    
    [Test]
    public async Task ShouldSignDataWithCertificate()
    {
        // random bytes
        var dataToSign = RandomNumberGenerator.GetBytes(1337);
        // Given a certificate name and data to sign
        
        // When I ask to sign the data with the certificate
        var signResult = await _signatureService.UseAzureToSign(dataToSign);
        
        // Then it should be successful
        Assert.That(signResult.Signature, Is.Not.Null);
        
    }
    
    [Test]
    public async Task ShouldVerifySignedData()
    {
        var dataToSign = RandomNumberGenerator.GetBytes(1337);
        // Given a certificate name and data to sign
        
        // When I ask to sign the data with the certificate
        var signResult = await _signatureService.UseAzureToSign(dataToSign);
        
        // and I ask to verify the signed data
        var verifyResult = await _signatureService.UseAzureToVerify(dataToSign, signResult.Signature);
        
        // Then it should be successful
        Assert.That(verifyResult.IsValid, Is.True);
        
    }

    [Test]
    public async Task ShouldBeAbleToVerifyNewlyGeneratedKey()
    {
        // Given a signed response
        var kekName = $"KEK-{Guid.NewGuid()}";
        var kekSignedResponse = await _keyVaultService.GenerateKekAsync(kekName);
        
        var kek = kekSignedResponse.Kek;
        var kekMarshaled = TokenHelper.SerializeObjectForAzureSignature(kek); ;
        var pem = kekSignedResponse.PemString;
        var kekAndPem = Encoding.UTF8.GetBytes(kekMarshaled + pem);
        
        // and a base64 encoded signature of the kek and pem
        var signature = kekSignedResponse.Base64EncodedSignature;
        var signatureBytes = Convert.FromBase64String(signature);
        
        // When I ask to verify the signature
        var verifyResult = await _signatureService.UseAzureToVerify(kekAndPem, signatureBytes);
        
        // Then it should be successful
        Assert.That(verifyResult.IsValid, Is.True);
    }

    [Test]
    public async Task ShouldBeAbleToGetAzureSigningCertificate()
    {
        // Given a certificate in the key vault
        // When I ask to get the signing certificate
        var cert = await _signatureService.GetAzureSigningCertificate();
        // Then it should be there
        Assert.That(cert, Is.Not.Null);
    }
}