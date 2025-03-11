using Infrastructure;
using Infrastructure.Interfaces;
using Test.TestHelpers;

namespace Test;

public class TestKeyVaultService
{
    private ITokenService _tokenService;
    private IKeyVaultService _keyVaultService;
    private FakeHsm _hsm;
    
    [SetUp]
    public void Setup()
    {
        _tokenService = new TokenService();
        _keyVaultService = new KeyVaultService(_tokenService);
        _hsm = new FakeHsm();
    }

    [Test]
    public async Task ShouldBePossibleToEncryptKeyWithKekAndUpload()
    {
        // Given a Key Encryption Key and transfer blob
        var kekName = $"KEK-{Guid.NewGuid()}";
        var kek = _keyVaultService.GenerateKek(kekName).Value;
        var transferBlob = _hsm.SimulateHsm(kek);
        var newKeyName = $"customer-KEY-{Guid.NewGuid()}";
        
        // When is ask to upload it
        var kvRes = await _keyVaultService.UploadKey(newKeyName, transferBlob, kek.Id.ToString());
        
        // Then it should be successful
        Assert.True(kvRes.Contains("key"));
    } 
}