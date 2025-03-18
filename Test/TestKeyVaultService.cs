using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using Test.TestHelpers;

namespace Test;

public class TestKeyVaultService
{
    private ITokenService _tokenService;
    private IKeyVaultService _keyVaultService;
    private IConfiguration _configuration; 
    
    [SetUp]
    public void Setup()
    {
        TestHelper.CreateTestConfiguration();
        _tokenService = new TokenService();
        IHttpClientFactory httpClientFactory = new FakeHttpClientFactory();
        _configuration = TestHelper.CreateTestConfiguration();
        _keyVaultService = new KeyVaultService(_tokenService, httpClientFactory,_configuration);
    }

    [Test]
    public async Task ShouldBePossibleToEncryptKeyWithKekAndUpload()
    {
        // Given a Key Encryption Key and transfer blob
        var kekName = $"KEK-{Guid.NewGuid()}";
        var kek = _keyVaultService.GenerateKek(kekName).Value;
        var transferBlob = FakeHsm.SimulateHsm(kek);
        var newKeyName = $"customer-KEY-{Guid.NewGuid()}";
        
        // When is ask to upload it
        var kvRes = await _keyVaultService.UploadKey(newKeyName, transferBlob, kek.Id.ToString());
        
        // Then it should be successful
        Assert.That(kvRes.Attributes.Enabled, Is.True);
    } 
}