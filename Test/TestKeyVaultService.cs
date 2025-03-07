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
        var kekName = $"kek-{Guid.NewGuid()}";

        var kek = _keyVaultService.GenerateKek(kekName).Value;

        var transferBlob = _hsm.SimulateHsm(kek);

        var newKeyName = $"new-key-{Guid.NewGuid()}";

        var kvRes = await _keyVaultService.ImportKey(newKeyName, transferBlob, kek.Id.ToString());
        
        Assert.True(kvRes.Contains("key"));
    } 
}