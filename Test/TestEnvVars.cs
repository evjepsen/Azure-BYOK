using Test.TestHelpers;

namespace Test;

public class TestEnvVars
{
    [SetUp]
    public void Setup()
    {
        TestHelper.LoadEnvVariables();
    }

    [Test]
    public void ShouldVaultUriExist()
    {
        // Given a DotNetEnv
        // When I ask for An environment variable
        var vaultUri = Environment.GetEnvironmentVariable("VAULT_URI");
        // Then it should be there
        Assert.IsNotNull(vaultUri);
    } 
}