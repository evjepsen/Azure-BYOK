using Infrastructure;
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
        _certificateCache = new CertificateCache();
    }
    
    [Test]
    public void ShouldThrowExceptionWhenCertificateNotFound()
    {
        // Given a certificate cache 
        // When I ask to get the certificate
        // Then it should throw an exception
        Assert.Throws<InvalidOperationException>(() => _certificateCache.GetCertificate());
    }

    [Test]
    public void ShouldBePossibleToAddACertificate()
    {
        // Given a certificate cache and a certificate
        var certificate = TestHelper.CreateCertificate();
        // When I ask to add it
        _certificateCache.AddCertificate(certificate);
        // Then it should be added
        var retrievedCertificate = _certificateCache.GetCertificate();
        // And the retrieved certificate should be the same as the original
        Assert.That(retrievedCertificate, Is.EqualTo(certificate));
    }
}