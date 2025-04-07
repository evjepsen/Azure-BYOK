using System.Security.Cryptography;
using System.Text;
using Infrastructure;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Test.TestHelpers;

namespace Test;

[TestFixture]
[TestOf(typeof(CertificateService))]
public class TestCertificateService
{
    
    private ICertificateService _certificateService;
    private IKeyVaultService _keyVaultService;
    
    [SetUp]
    public void Setup()
    {
        TestHelper.CreateTestConfiguration();
        IHttpClientFactory httpClientFactory = new FakeHttpClientFactory();
        var configuration = TestHelper.CreateTestConfiguration();
        // _tokenService = new TokenService(new NullLoggerFactory());
        var applicationOptions = TestHelper.CreateApplicationOptions(configuration);
        var tokenService = new TokenService(new NullLoggerFactory());
        _keyVaultService = new KeyVaultService(tokenService, httpClientFactory, applicationOptions, new NullLoggerFactory());
        _certificateService= new CertificateService(httpClientFactory, applicationOptions, new NullLoggerFactory());
    }

    [Test]
    public async Task ShouldSignDataWithCertificate()
    {
        // random bytes
        var dataToSign = RandomNumberGenerator.GetBytes(1337);
        // Given a certificate name and data to sign
        
        // When I ask to sign the data with the certificate
        var signResult = await _certificateService.SignAsync(dataToSign);
        
        // Then it should be successful
        Assert.That(signResult.Signature, Is.Not.Null);
        
    }
    
    [Test]
    public async Task ShouldVerifySignedData()
    {
        var dataToSign = RandomNumberGenerator.GetBytes(1337);
        // Given a certificate name and data to sign
        
        // When I ask to sign the data with the certificate
        var signResult = await _certificateService.SignAsync(dataToSign);
        
        // and I ask to verify the signed data
        var verifyResult = await _certificateService.VerifyAsync(dataToSign, signResult.Signature);
        
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
        var kekMarshaled = TokenHelper.SerializeJsonObject(kek);
        var pem = kekSignedResponse.PemString;
        var kekAndPem = Encoding.UTF8.GetBytes(kekMarshaled + pem);
        // and a base64 encoded signature of the kek and pem
        var signature = kekSignedResponse.Base64EncodedSignature;
        var signatureBytes = Convert.FromBase64String(signature);
        
        // When I ask to verify the signature
        var verifyResult = await _certificateService.VerifyAsync(kekAndPem, signatureBytes);
        
        // Then it should be successful
        Assert.That(verifyResult.IsValid, Is.True);


    }
}