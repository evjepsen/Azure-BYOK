using Infrastructure.Options;
using Microsoft.Extensions.Options;
using Test.TestHelpers;

namespace Test;

[TestFixture]
public class TestEnvVars
{
    private IOptions<ApplicationOptions> _applicationOptions;
    
    [SetUp]
    public void Setup()
    {
        var configuration = TestHelper.CreateTestConfiguration();
        _applicationOptions = TestHelper.CreateApplicationOptions(configuration);
    }

    [Test]
    public void ShouldVaultUriExist()
    {
        // When I ask for An environment variable
        var vaultUri = _applicationOptions.Value.VaultUri;
        // Then it should be there
        Assert.IsNotNull(vaultUri);
    } 
    [Test]
    public void ShouldSubscriptionIdExist()
    {
        // When I ask for An environment variable
        var subscriptionId= _applicationOptions.Value.SubscriptionId;
        // Then it should be there
        Assert.IsNotNull(subscriptionId);
    } 
    [Test]
    public void ShouldResourceGroupExist()
    {
        // When I ask for An environment variable
        var resourceGroup = _applicationOptions.Value.ResourceGroupName;
        // Then it should be there
        Assert.IsNotNull(resourceGroup);
    } 
    [Test]
    public void ShouldKeyVaultResourceExist()
    {
        // When I ask for An environment variable
        var kvResource= _applicationOptions.Value.KeyVaultResourceName;
        // Then it should be there
        Assert.IsNotNull(kvResource);
    } 
}