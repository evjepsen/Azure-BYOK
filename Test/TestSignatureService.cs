using System.Security.Cryptography;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Test.TestHelpers;

namespace Test;

[TestFixture]
[TestOf(typeof(Infrastructure.TestSignatureService))]
public class TestSignatureService
{
    private ISignatureService _signatureService;
    private ICertificateCache _certificateCache;

    [SetUp]
    public void Setup()
    {
        var configuration = TestHelper.CreateTestConfiguration();
        var applicationOptions = TestHelper.CreateApplicationOptions(configuration);
        
        // Initialize the certificate cache and signature service
        _certificateCache = new Infrastructure.TestCertificateCache(new NullLoggerFactory(), applicationOptions);
        _signatureService = new Infrastructure.TestSignatureService(_certificateCache, new NullLoggerFactory());
    }
    
    [Test]
    public void ShouldThrowExceptionWhenCertificateNotFound()
    {
        var data = "Test data"u8.ToArray();
        // Given a signature service 
        // When I ask check whether a signature is valid
        var exception = Assert.Throws<InvalidOperationException>(() => _signatureService.IsSignatureValid("Test signature", data));

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
        var isValid = _signatureService.IsSignatureValid(signature, data);
        
        // Then it should be successful
        Assert.That(isValid, Is.True);
    }
}