using System.Security.Cryptography;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Test.TestHelpers;

namespace Test;

[TestFixture]
[TestOf(typeof(SignatureService))]
public class SignatureServiceTest
{
    private ISignatureService _signatureService;
    private ICertificateCache _certificateCache;

    public SignatureServiceTest(ISignatureService signatureService, ICertificateCache certificateCache)
    {
        _signatureService = signatureService;
        _certificateCache = certificateCache;
    }

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
        var certificate = TestHelper.CreateCertificate(key);
        _certificateCache.AddCertificate(certificate);
        
        // When I ask to verify the signature
        var (signature, data) = TestHelper.CreateSignature(key);
        var isValid = _signatureService.IsSignatureValid(signature, data);
        
        // Then it should be successful
        Assert.That(isValid, Is.True);
    }
}