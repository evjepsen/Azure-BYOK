using Microsoft.Extensions.Configuration;

namespace Test;

public class TestEnvVars
{
    private IConfiguration _configuration; 
    
    [SetUp]
    public void Setup()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("testConfiguration.json", false, true);
        _configuration = builder.Build();
    }

    [Test]
    public void ShouldVaultUriExist()
    {
        // Given a DotNetEnv
        // When I ask for An environment variable
        var vaultUri = _configuration["VAULT_URI"];
        // Then it should be there
        Assert.IsNotNull(vaultUri);
    } 
}