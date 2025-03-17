using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using Test.TestHelpers;

namespace Test;

public class TestKeyVaultService
{
    private ITokenService _tokenService;
    private IKeyVaultService _keyVaultService;
    private FakeHsm _hsm;
    private IConfiguration _configuration; 
    
    [SetUp]
    public void Setup()
    {
        _tokenService = new TokenService();
        IHttpClientFactory httpClientFactory = new FakeHttpClientFactory();
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.azure.json", false, true);
        _configuration = builder.Build();
        _keyVaultService = new KeyVaultService(_tokenService, httpClientFactory,_configuration);
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