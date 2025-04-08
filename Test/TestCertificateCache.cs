using System.Security.Cryptography;
using Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Test.TestHelpers;

namespace Test;

[TestFixture]
[TestOf(typeof(Infrastructure.TestCertificateCache))]
public class TestCertificateCache
{
    private Infrastructure.TestCertificateCache _testCertificateCache;

    [SetUp]
    public void Setup()
    {
        var configuration = TestHelper.CreateTestConfiguration();
        var applicationOptions = TestHelper.CreateApplicationOptions(configuration);
        _testCertificateCache = new Infrastructure.TestCertificateCache(new NullLoggerFactory(), applicationOptions);
    }
    
    [Test]
    public void ShouldThrowExceptionWhenCertificateNotFound()
    {
        // Given a certificate cache 
        // When I ask to get the certificate
        var certificate = _testCertificateCache.GetCertificate();
        // Then it should throw an exception
        Assert.That(certificate, Is.Null);
    }

    [Test]
    public void ShouldBePossibleToAddACertificate()
    {
        // Given a certificate cache and a certificate
        var certificate = TestHelper.CreateCertificate(
            RSA.Create(), 
            "cn=Customer HSM",
            DateTimeOffset.Now,
            DateTimeOffset.Now.AddYears(1)
        );
        // When I ask to add it
        _testCertificateCache.AddCertificate(certificate);
        // Then it should be added
        var retrievedCertificate = _testCertificateCache.GetCertificate();
        // And the retrieved certificate should be the same as the original
        Assert.That(retrievedCertificate, Is.EqualTo(certificate));
    }
    
    [Test]
    public void ShouldSelfSignedCertificateShouldBeValid()
    {
        // Given a certificate cache and a self-signed certificate
        var certificate = TestHelper.CreateCertificate(
            RSA.Create(), 
            "cn=Customer HSM",
            DateTimeOffset.Now,
            DateTimeOffset.Now.AddYears(1)
        );
        
        // When I to validate it
        var isValid = _testCertificateCache.ValidateCertificate(certificate);
        
        // Then it should be valid
        Assert.That(isValid, Is.True);
    }
    
    [Test]
    public void ShouldSelfSignedCertificateThatHasTheWrongSubjectNotBeValid()
    {
        // Given a certificate cache and a self-signed certificate
        var certificate = TestHelper.CreateCertificate(
            RSA.Create(), 
            "cn=BYOK",
            DateTimeOffset.Now,
            DateTimeOffset.Now.AddYears(1)
        );
        
        // When I to validate it
        var isValid = _testCertificateCache.ValidateCertificate(certificate);
        
        // Then it should be valid
        Assert.That(isValid, Is.False);
    }
    
    [Test]
    public void ShouldSelfSignedCertificateThatIsNotLongerValidNotBeValid()
    {
        // Given a certificate cache and a self-signed certificate
        var certificate = TestHelper.CreateCertificate(
            RSA.Create(), 
            "cn=Customer HSM",
            DateTimeOffset.Now.AddYears(-2),
            DateTimeOffset.Now.AddYears(-1)
        );
        
        // When I to validate it
        var isValid = _testCertificateCache.ValidateCertificate(certificate);
        
        // Then it should be valid
        Assert.That(isValid, Is.False);
    }
    
    [Test]
    public void ShouldSelfSignedCertificateThatIsNotValidYetNotBeValid()
    {
        // Given a certificate cache and a self-signed certificate
        var certificate = TestHelper.CreateCertificate(
            RSA.Create(), 
            "cn=Customer HSM",
            DateTimeOffset.Now.AddYears(1),
            DateTimeOffset.Now.AddYears(2)
        );
        
        // When I to validate it
        var isValid = _testCertificateCache.ValidateCertificate(certificate);
        
        // Then it should be valid
        Assert.That(isValid, Is.False);
    }
}