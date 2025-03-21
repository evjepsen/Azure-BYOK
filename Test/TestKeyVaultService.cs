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
        var kek = await _keyVaultService.GenerateKek(kekName);
        var transferBlob = FakeHsm.SimulateHsm(kek);
        var newKeyName = $"customer-KEY-{Guid.NewGuid()}";
        
        // When is ask to upload it
        var kvRes = await _keyVaultService.UploadKey(newKeyName, transferBlob, kek.Id.ToString());
        
        // Then it should be successful
        Assert.That(kvRes.Attributes.Enabled, Is.True);
    }

    [Test]
    public async Task ShouldBePossibleToGetPublicKeyOfKekAsPem()
    {
        // Given a Key Encryption Key
        var kekId = $"KEK-{Guid.NewGuid()}";
        var kek = await _keyVaultService.GenerateKek(kekId);
        
        // When is ask to get the public key as PEM
        var gotPem = await _keyVaultService.DownloadPublicKekAsPemAsync(kekId);
        
        // Then the PEM should be the same as the one we generated
        var wantPem = kek.Key.ToRSA().ExportRSAPublicKeyPem();
        Assert.That(gotPem.PemString, Is.EqualTo(wantPem));
        
    }
}