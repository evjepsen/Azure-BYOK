using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

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
    
    public static X509Certificate2 CreateCertificate(RSA rsa)
    {
        var req = new CertificateRequest("cn=foobar", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
        var certData = cert.Export(X509ContentType.Cert);
        return new X509Certificate2(certData);
    }

    public static (string signature, string data) CreateSignature(RSA key)
    {
        var data = "Test data";
        var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
        var signature = key.SignData(dataBytes, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
        return (Convert.ToBase64String(signature), Convert.ToBase64String(dataBytes));
    }
}