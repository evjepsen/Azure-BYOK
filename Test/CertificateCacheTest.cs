using System.Security.Cryptography;
using Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Test.TestHelpers;

namespace Test;

[TestFixture]
[TestOf(typeof(CertificateCache))]
public class CertificateCacheTest
{
    private CertificateCache _certificateCache;

    [SetUp]
    public void Setup()
    {
        var configuration = TestHelper.CreateTestConfiguration();
        var applicationOptions = TestHelper.CreateApplicationOptions(configuration);
        _certificateCache = new CertificateCache(new NullLoggerFactory(), applicationOptions);
    }
    
    [Test]
    public void ShouldThrowExceptionWhenCertificateNotFound()
    {
        // Given a certificate cache 
        // When I ask to get the certificate
        var certificate = _certificateCache.GetCertificate();
        // Then it should throw an exception
        Assert.That(certificate, Is.Null);
    }

    [Test]
    public void ShouldBePossibleToAddACertificate()
    {
        // Given a certificate cache and a certificate
        var certificate = TestHelper.CreateCertificate(RSA.Create());
        // When I ask to add it
        _certificateCache.AddCertificate(certificate);
        // Then it should be added
        var retrievedCertificate = _certificateCache.GetCertificate();
        // And the retrieved certificate should be the same as the original
        Assert.That(retrievedCertificate, Is.EqualTo(certificate));
    }
    
    [Test]
    public void SelfSignedCertificateShouldBeValid()
    {
        // Given a certificate cache and a self-signed certificate
        var key = RSA.Create();
        var certificate = TestHelper.CreateCertificate(key);
        
        // When I to validate it
        var isValid = _certificateCache.ValidateCertificate(certificate);
        
        // Then it should be valid
        Assert.That(isValid, Is.True);
    }
}