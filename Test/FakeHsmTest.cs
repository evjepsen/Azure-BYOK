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
        var tokenService = new TokenService();
        IHttpClientFactory httpClientFactory = new FakeHttpClientFactory();
        var configuration = TestHelper.CreateTestConfiguration();
        var keyVaultService = new KeyVaultService(tokenService, httpClientFactory,configuration);
        // Given a key vault service
        
        var kekName = $"KEK-{Guid.NewGuid()}";
        var kek = await keyVaultService.GenerateKekAsync(kekName);
        // Given a Key Encryption Key 
        
        // When I ask to generate a blob
        var transferBlob = _fakeHsm.GeneratePrivateKeyForBlob(kek.Key.ToRSA());
        
        // Then it should be successful
        Assert.That(transferBlob, Is.Not.Null);
    }
}