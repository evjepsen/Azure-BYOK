using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using API.Controllers;
using Azure.Security.KeyVault.Certificates;
using Infrastructure.Exceptions;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Test.TestHelpers;

namespace Test.Controllers;

[TestFixture]
[TestOf(typeof(CertificateController))]
public class TestCertificateController
{
    private CertificateController _certificateController;
    private Mock<ISignatureService> _mockSignatureService;
    private Mock<ILogger<CertificateController>> _mockLogger;
    private Mock<ICertificateCache> _mockCertificateCache;


    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<CertificateController>>();
        _mockSignatureService = new Mock<ISignatureService>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(logger => logger.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);
        
        _mockCertificateCache = new Mock<ICertificateCache>();
        _certificateController = new CertificateController(_mockCertificateCache.Object,mockLoggerFactory.Object,_mockSignatureService.Object);
    }


    [Test]
    public async Task ShouldUploadEmptyCertificateReturnBadRequest()
    {
        
        // given a certificate file
        var certificateFile = new Mock<IFormFile>();
        // with length of 0
        certificateFile.Setup(f => f.Length).Returns(0);
        
        // when I upload the certificate
        var result = await _certificateController.UploadCustomerCertificate(certificateFile.Object);
        // then it should return a bad request
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        
        // safe cast status code
        var gotStatusCode = (ObjectResult) result;
        Assert.That(gotStatusCode.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        
        // and the logger should log an error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("Certificate file is null or empty.", o.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );

    }

    [Test]
    public async Task ShouldInvalidMemoryStreamReturnBadRequest()
    {
       // given an certificate file
        var certificateFile = new Mock<IFormFile>();
        // with length of 1
        certificateFile.Setup(f => f.Length).Returns(1);
        // and an that cannot copied to memory stream
        certificateFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Error reading certificate file"));
        
        // when I upload the certificate
        var result = await _certificateController.UploadCustomerCertificate(certificateFile.Object);
        
        // then it should return BadRequest
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        
        // safe cast status code
        var gotStatusCode = (ObjectResult) result;
        Assert.That(gotStatusCode.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        
        // and the logger should log an error since the file could not be read
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("Error reading certificate file: Error reading certificate file", o.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
    
    [Test]
    public async Task ShouldExpiredCertificateReturnBadRequest()
    {

        // Given a expired certificate file as IFormFile
        var notBefore = DateTime.UtcNow.AddDays(-2);
        var notAfter = DateTime.UtcNow.AddDays(-1);
        var fakeIForm = TestHelper.CreateCertTestFile(notBefore, notAfter);
        
        // when I upload the certificate
        var result = await _certificateController.UploadCustomerCertificate(fakeIForm);
        
        // then it should return BadRequest
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        
        
        var gotStatusCode = (ObjectResult) result; // safe cast status code
        // and the status code should be 400
        Assert.That(gotStatusCode.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        
        // and the logger should log an error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("Certificate has expired", o.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
        // However, the logger should not log the exception
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("Certificate is not yet valid", o.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never
        );
    }
    
    [Test]
    public async Task ShouldNotYetValidCertificateReturnBadRequest()
    {
        // Given a certificate file as IFormFile
        var notBefore = DateTime.UtcNow.AddDays(1);
        var notAfter = DateTime.UtcNow.AddDays(2);
        var fakeIForm = TestHelper.CreateCertTestFile(notBefore, notAfter);
        
        // when I upload the certificate
        var result = await _certificateController.UploadCustomerCertificate(fakeIForm);
        
        // then it should return BadRequest
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        
        
        var gotStatusCode = (ObjectResult) result; // safe cast status code
        // and the status code should be 400
        Assert.That(gotStatusCode.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        
        // and the logger not should the expired error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("Certificate has expired", o.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never
        );
        // However, it should log the not yet valid error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("Certificate is not yet valid", o.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
    
    [Test]
    public async Task ShouldAddingInvalidCertificateReturnBadRequest()
    {
        // Given a valid certificate file as IFormFile
        var notBefore = DateTime.UtcNow.AddDays(-1);
        var notAfter = DateTime.UtcNow.AddDays(1);
        var fakeIForm = TestHelper.CreateCertTestFile(notBefore, notAfter);
        
        // and the certificate cache throws an exception
        _mockCertificateCache.Setup(mockCache=> mockCache.AddCertificate(It.IsAny<X509Certificate2>()))
            .Throws(new InvalidOperationException("Certificate was not valid"));
        
        // when I upload the certificate
        var result = await _certificateController.UploadCustomerCertificate(fakeIForm);
        
        // then it should return BadRequest
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        
        
        var gotStatusCode = (ObjectResult) result; // safe cast status code
        // and the status code should be 400
        Assert.That(gotStatusCode.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        
        // and the logger should log an error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("Certificate was not valid", o.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Test]
    public async Task ShouldValidCustomerCertificateWhenUploadedReturnOk()
    {
        // Given a valid certificate file as IFormFile
        var notBefore = DateTime.UtcNow.AddDays(-1);
        var notAfter = DateTime.UtcNow.AddDays(1);
        var fakeIForm = TestHelper.CreateCertTestFile(notBefore, notAfter);
        
        // when I upload the certificate
        var result = await _certificateController.UploadCustomerCertificate(fakeIForm);
        
        // then it should return Ok
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        
        var gotStatusCode = (ObjectResult) result; // safe cast status code
        // and the status code should be 200
        Assert.That(gotStatusCode.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        
        // and the logger should log an information message
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("Certificate was uploaded successfully", o.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    /// <summary>
    /// Tests for the GetAzureSigningCertificate method in the CertificateController
    /// </summary>
    [Test]
    public async Task ShouldGetAzureSigningCertificateWhenFoundReturnOk()
    {
        _mockSignatureService.Setup(mockSignatureService =>
                mockSignatureService.GetKeyVaultCertificateAsX509PemString())
            .ReturnsAsync("");
        
        // when I upload the certificate
        var result = await _certificateController.GetAzureSigningCertificate();
        
        // then it should return Ok
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        
        var gotStatusCode = (ObjectResult) result; // safe cast status code
        // and the status code should be 200
        Assert.That(gotStatusCode.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("Certificate was converted to PEM successfully", o.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    [Test]
    public async Task ShouldACryptographicInvalidCertificateFromAzureReturnInternalServerError()
    {
        _mockSignatureService.Setup(mockSignatureService =>
            mockSignatureService.GetKeyVaultCertificateAsX509PemString())
            .ThrowsAsync(new CryptographicException("Certificate was cryptographically invalid"));
        
        // when I upload the certificate
        var result = await _certificateController.GetAzureSigningCertificate();
        
        // then it should return InternalServerError
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        var gotStatusCode = (ObjectResult) result; // safe cast status code
        // and the status code should be 500
        Assert.That(gotStatusCode.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
   
        // but the logger should log that the certificate was cryptographically invalid
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("Certificate was cryptographically invalid", o.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
            );
    }



    [Test]
    public async Task ShouldNotRetrievingCertificateFromAzureReturnBadRequest()
    {
        
        _mockSignatureService.Setup(mockSignatureService =>
                mockSignatureService.GetKeyVaultCertificateAsX509PemString())
            .ThrowsAsync(new ResourceNotFoundException("Error retrieving certificate from Azure"));
        
        // when I upload the certificate
        var result = await _certificateController.GetAzureSigningCertificate();
        // then it should return 404
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        
        var gotStatusCode = (ObjectResult) result; // safe cast status code
        // and the status code should be 404
        Assert.That(gotStatusCode.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        
        // and the logger should log an error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => string.Equals("Certificate was not found", o.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
    

    [TearDown]
    public void TearDownController()
    {
        _certificateController.Dispose();
    }
}

