using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Test.TestHelpers;

namespace Test;

[TestFixture]
[TestOf(typeof(SignatureService))]
public class SignatureServiceTest
{
    private SignatureService _signatureService;
    private CertificateCache _certificateCache;

    [SetUp]
    public void Setup()
    {
        // Initialize the certificate cache and signature service
        _certificateCache = new CertificateCache();
        _signatureService = new SignatureService(_certificateCache, new NullLoggerFactory());
    }
    
    [Test]
    public void ShouldThrowExceptionWhenCertificateNotFound()
    {
        // Given a signature service 
        // When I ask check whether a signature is valid
        var exception = Assert.Throws<InvalidOperationException>(() => _signatureService.IsSignatureValid("Test signature", "Test data"));

        // Then it should throw an exception
        Assert.That(exception.Message, Is.EqualTo("No certificate was found."));
    }
    
    [Test]
    public void ShouldBePossibleToVerifySignature()
    {
        // Given a signature service and a certificate stored in the cache (Which has been used to sign a message)
        var key = RSA.Create();
        var certificate = TestHelper.CreateCertificate(key);
        _certificateCache.AddCertificate(certificate);
        
        // When I ask to verify the signature
        var (signature, data) = TestHelper.CreateSignature(key);
        var isValid = _signatureService.IsSignatureValid(signature, data);
        
        // Then it should be successful
        Assert.That(isValid, Is.True);
    }
}