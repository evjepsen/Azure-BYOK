using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Sprache;

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
    
    public static X509Certificate2 CreateCertificate()
    {
        var ecdsa = ECDsa.Create(); // generate asymmetric key pair
        var req = new CertificateRequest("cn=foobar", ecdsa, HashAlgorithmName.SHA256);
        return req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
    }
}