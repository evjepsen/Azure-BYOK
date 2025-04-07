using System.Security.Cryptography;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Test.TestHelpers;

namespace Test;

[TestFixture]
[TestOf(typeof(CertificateService))]
public class TestCertificateService
{
    
    private ICertificateService _certificateService;
    
    [SetUp]
    public void Setup()
    {
        TestHelper.CreateTestConfiguration();
        IHttpClientFactory httpClientFactory = new FakeHttpClientFactory();
        var configuration = TestHelper.CreateTestConfiguration();
        // _tokenService = new TokenService(new NullLoggerFactory());
        var applicationOptions = TestHelper.CreateApplicationOptions(configuration);
        _certificateService= new CertificateService(httpClientFactory, applicationOptions, new NullLoggerFactory());
    }

    [Test]
    public async Task ShouldSignDataWithCertificate()
    {
        // random bytes
        var dataToSign = RandomNumberGenerator.GetBytes(1337);
        // Given a certificate name and data to sign
        
        // When I ask to sign the data with the certificate
        var signResult = await _certificateService.SignKeyWithCertificateAsync(dataToSign);
        
        // Then it should be successful
        Assert.That(signResult.Signature, Is.Not.Null);
        
    }
    
    [Test]
    public async Task ShouldVerifySignedData()
    {
        var dataToSign = RandomNumberGenerator.GetBytes(1337);
        // Given a certificate name and data to sign
        
        // When I ask to sign the data with the certificate
        var signResult = await _certificateService.SignKeyWithCertificateAsync(dataToSign);
        
        // and I ask to verify the signed data
        var verifyResult = await _certificateService.VerifyCertificateAsync(dataToSign, signResult.Signature);
        
        // Then it should be successful
        Assert.That(verifyResult.IsValid, Is.True);
        
    }

}