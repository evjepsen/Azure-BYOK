using FakeHSM.Interfaces;
using Infrastructure;
using Test.TestHelpers;
using FakeHsm = FakeHSM.FakeHsm;

namespace Test;

[TestFixture]
[TestOf(typeof(FakeHsm))]
public class FakeHsmTest
{
    private IFakeHsm _fakeHsm;

    [SetUp]
    public void Setup()
    {
        _fakeHsm = new FakeHsm();
    }
    [Test]
    public async Task ShouldBlobBeGenerated()
    {
        var configuration = TestHelper.CreateTestConfiguration();
        var tokenService = new TokenService(TestHelper.CreateJwtOptions(configuration));
        IHttpClientFactory httpClientFactory = new FakeHttpClientFactory();
        var keyVaultService = new KeyVaultService(tokenService, httpClientFactory, TestHelper.CreateApplicationOptions(configuration));
        
        var kekName = $"KEK-{Guid.NewGuid()}";
        var kek = await keyVaultService.GenerateKekAsync(kekName);
        // Given a key vault service and Key Encryption Key 
        
        // When I ask to generate a blob
        var transferBlob = _fakeHsm.GeneratePrivateKeyForBlob(kek.Key.ToRSA());
        
        // Then it should be successful
        Assert.That(transferBlob, Is.Not.Null);
    }
}