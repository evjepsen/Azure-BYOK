using Microsoft.Extensions.Configuration;
using Test.TestHelpers;

namespace Test;

public class TestEnvVars
{
    private IConfiguration _configuration; 
    
    [SetUp]
    public void Setup()
    {
        _configuration = TestHelper.LoadEnvVariables();
    }

    [Test]
    public void ShouldVaultUriExist()
    {
        // When I ask for An environment variable
        var vaultUri = _configuration["VAULT_URI"];
        // Then it should be there
        Assert.IsNotNull(vaultUri);
    } 
    [Test]
    public void ShouldSubscriptionIdExist()
    {
        // When I ask for An environment variable
        var subscriptionId= _configuration["SUBSCRIPTION_ID"];
        // Then it should be there
        Assert.IsNotNull(subscriptionId);
    } 
    [Test]
    public void ShouldResourceGroupExist()
    {
        // When I ask for An environment variable
        var resourceGroup = _configuration["RESOURCE_GROUP_NAME"];
        // Then it should be there
        Assert.IsNotNull(resourceGroup);
    } 
    [Test]
    public void ShouldKeyVaultResourceExist()
    {
        // When I ask for An environment variable
        var KVResource= _configuration["KV_RESOURCE_NAME"];
        // Then it should be there
        Assert.IsNotNull(KVResource);
    } 
}