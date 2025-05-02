using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Azure.Security.KeyVault.Certificates;
using Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;

namespace Test.TestHelpers;

public static class TestHelper
{
    public static IConfiguration CreateTestConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.azure.json", false)
            .AddJsonFile("appsettings.json", false);
        var res = builder.Build();
        return res;
    }

    public static IOptions<JwtOptions> CreateJwtOptions(IConfiguration configuration)
    {
        var jwtOptions = new JwtOptions();
        configuration.GetSection(JwtOptions.Jwt).Bind(jwtOptions);
        return Options.Create(jwtOptions);
    }
    
    public static IOptions<ApplicationOptions> CreateApplicationOptions(IConfiguration configuration)
    {
        var applicationOptions = new ApplicationOptions();
        configuration.GetSection(ApplicationOptions.Application).Bind(applicationOptions);
        return Options.Create(applicationOptions);
    }
    
    public static X509Certificate2 CreateCertificate(RSA rsa, string subjectName, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var req = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(notBefore, notAfter);
        var certData = cert.Export(X509ContentType.Cert);
        return new X509Certificate2(certData);
    }

    public static (string signature, byte[] data) CreateSignature(RSA key)
    {
        var data = "Test data";
        var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
        var signature = key.SignData(dataBytes, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
        return (Convert.ToBase64String(signature), dataBytes);
    }
    
    /// <summary>
    /// Helper method to create a fake IFormFile with a self-signed certificate
    /// </summary>
    /// <param name="notBefore">DateTime of start of validity</param>
    /// <param name="notAfter">DateTime of expiration</param>
    /// <returns>A Fake IFormFile containing a test certificate</returns>
    public static IFormFile CreateCertTestFile(DateTime notBefore, DateTime notAfter)
    {
        // create a self-signed certificate, in pem format
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = request.CreateSelfSigned(notBefore,notAfter);
        var certData = cert.Export(X509ContentType.Pfx);
        var certFile = new Mock<IFormFile>();
        certFile.Setup(f => f.Length).Returns(certData.Length);
        var stream = new MemoryStream(certData);
        certFile.Setup(f => f.OpenReadStream()).Returns(stream);
        certFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, CancellationToken>((s, c) => stream.CopyTo(s));
        
        return certFile.Object;
    }
}