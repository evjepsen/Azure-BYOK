using Infrastructure;
using Test.TestHelpers;
using FakeHsm = FakeHSM.FakeHsm;

namespace Test;

[TestFixture]
[TestOf(typeof(FakeHsm))]
public class FakeHsmTest
{
    private FakeHsm _fakeHsm;

    [SetUp]
    public void Setup()
    {
        _fakeHsm = new FakeHsm();
    }
    [Test]
    public void HsmShouldBeEmptyAfterInit()
    {
        // given a newly initialized HSM
        
        // when I ask for the number of keys
        var numOfKeys = _fakeHsm.GetKeyIds().Count;
        
        // then it should be empty
        Assert.That(numOfKeys, Is.Zero);
    }
    [Test]
    public void HsmShouldHaveOneKeyAfterKeyGeneration()
    {
        _fakeHsm.GenerateRsaKey(2048);
        // given a hsm with one key
        
        // when I ask for the number of keys
        var numOfKeys = _fakeHsm.GetKeyIds().Count;
        
        // then it should have one key
        Assert.That(numOfKeys, Is.EqualTo(1));
    }
    [Test]
    public void ShouldBeAbleToGetPublicKeyWithId()
    {
        var (id, wantPk) = _fakeHsm.GenerateRsaKey(2048);
        // given a hsm with one key
        
        // when I ask for the public key of the key
        var gotPk = _fakeHsm.GetPublicKeyOfId(id);
        
        // then it should be the same as the one I generated
        Assert.That(gotPk, Is.EqualTo(wantPk));
    }

    [Test]
    public async Task ShouldBlobBeGenerated()
    {
        TestHelper.CreateTestConfiguration();
        var _tokenService = new TokenService();
        IHttpClientFactory httpClientFactory = new FakeHttpClientFactory();
        var _configuration = TestHelper.CreateTestConfiguration();
        var _keyVaultService = new KeyVaultService(_tokenService, httpClientFactory,_configuration);
        // Given a key vault service
        
        var kekName = $"KEK-{Guid.NewGuid()}";
        var kek = await _keyVaultService.GenerateKekAsync(kekName);
        var hsm = new FakeHsm();
        // var newKeyName = $"customer-KEY-{Guid.NewGuid()}";
        // Given a Key Encryption Key 
        
        // When I ask to generate a blob
        var transferBlob = hsm.GenerateBlob(kek);
        
        // Then it should be successful
        Assert.That(transferBlob, Is.Not.Null);
    }
}