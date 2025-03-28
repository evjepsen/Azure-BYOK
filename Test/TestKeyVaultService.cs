using Azure.Security.KeyVault.Keys;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using Test.TestHelpers;

namespace Test;


public class TestKeyVaultService
{
    private ITokenService _tokenService;
    private KeyVaultService _keyVaultService;
    private IConfiguration _configuration; 
    private KeyVaultManagementService _keyVaultManagementService;
    
    [SetUp]
    public void Setup()
    {
        TestHelper.CreateTestConfiguration();
        IHttpClientFactory httpClientFactory = new FakeHttpClientFactory();
        _configuration = TestHelper.CreateTestConfiguration();
        _tokenService = new TokenService(_configuration);
        _keyVaultService = new KeyVaultService(_tokenService, httpClientFactory,_configuration);
        _keyVaultManagementService = new KeyVaultManagementService(_configuration);
    }

    [Test]
    public async Task ShouldBePossibleToCreateAKek()
    {
        // Given a key vault service
        // When I ask to create a key encryption key
        var kekName = $"KEK-{Guid.NewGuid()}";
        var kek = await _keyVaultService.GenerateKekAsync(kekName);
        // Then it should be created and have the correct attributes
        Assert.That(kek.KeyOperations, Has.Count.EqualTo(1));
        Assert.That(kek.KeyOperations, Does.Contain(KeyOperation.Import));
        Assert.That(kek.KeyType, Is.EqualTo(KeyType.RsaHsm));
        Assert.That(kek.Properties.Enabled, Is.True);
        Assert.That(kek.Properties.Exportable, Is.False);
        Assert.That(kek.Properties.ExpiresOn, Is.LessThanOrEqualTo(DateTimeOffset.Now.AddHours(12)));
    }

    [Test]
    public async Task ShouldBePossibleToEncryptKeyWithKekAndUpload()
    {
        // Given a Key Encryption Key and transfer blob
        var kekName = $"KEK-{Guid.NewGuid()}";
        var kek = await _keyVaultService.GenerateKekAsync(kekName);
        var hsm = new FakeHSM.FakeHsm();
        var encryptedKek = hsm.GeneratePrivateKeyForBlob(kek.Key.ToRSA());
        var newKeyName = $"customer-KEY-{Guid.NewGuid()}";
        
        // When is ask to upload it
        var kvRes = await _keyVaultService.UploadKey(newKeyName, encryptedKek, kek.Id.ToString());
        
        // Then it should be successful
        Assert.That(kvRes.Attributes.Enabled, Is.True);
    }

    [Test]
    public async Task ShouldBePossibleToGetPublicKeyOfKekAsPem()
    {
        // Given a Key Encryption Key
        var kekId = $"KEK-{Guid.NewGuid()}";
        var kek = await _keyVaultService.GenerateKekAsync(kekId);
        
        // When I ask to get the public key as PEM
        var gotPem = await _keyVaultService.DownloadPublicKekAsPemAsync(kekId);
        
        // Then the PEM should be the same as the one we generated
        var wantPem = kek.Key.ToRSA().ExportRSAPublicKeyPem();
        Assert.That(gotPem.PemString, Is.EqualTo(wantPem));
        
    }

    [Test]
    public async Task ShouldBeAbleToDeleteAKek()
    {
        // Given a Key Encryption Key 
        var kekName = $"Random-delete-{Guid.NewGuid()}";
        var kek = await _keyVaultService.GenerateKekAsync(kekName);
        
        // When I ask to delete it
        var delOp = await _keyVaultService.DeleteKekAsync(kekName);
        
        // Then it should be deleted and the key should be the same
        Assert.That(delOp.Properties.Id, Is.EqualTo(kek.Id));
    }
    
    [Test]
    public async Task ShouldAbleToPurgeADeletedKek()
    {
        // Given a Key Encryption Key
        var kekName = $"Random-purge-{Guid.NewGuid()}";
        await _keyVaultService.GenerateKekAsync(kekName);
        
        // Which I delete
        await _keyVaultService.DeleteKekAsync(kekName);
        
        // When I ask to purge it
        // Then it should be purged if the key vault has purge protection
        if (!_keyVaultManagementService.DoesKeyVaultHavePurgeProtection())
        {
            var purgeKekOperation = await _keyVaultService.PurgeDeletedKekAsync(kekName);
            Assert.That(purgeKekOperation.Status, Is.EqualTo(204));
        }
        // Otherwise, purging is not possible and an exception should be thrown 
        else
        {
            Assert.ThrowsAsync<Azure.RequestFailedException>(async () => await _keyVaultService.PurgeDeletedKekAsync(kekName));
        }
    }
}