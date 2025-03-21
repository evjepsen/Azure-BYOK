using Azure.Security.KeyVault.Keys;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Test.TestHelpers;
using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.Rest;

namespace Test;


public class TestKeyVaultService
{
    private ITokenService _tokenService;
    private IKeyVaultService _keyVaultService;
    private IConfiguration _configuration; 
    private IKeyVaultManagementClient _keyVaultManagementClient;
    private DefaultAzureCredential _tokenCredential;

    public async Task<bool> DoesKeyVaultHavePurgeProtectionAsync()
    {
        // Get management token.
        var tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions());
        var tokenRequestContext = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
        var accessToken = await tokenCredential.GetTokenAsync(tokenRequestContext);

        // Create ServiceClientCredentials credentials.
        ServiceClientCredentials credentials = new TokenCredentials(accessToken.Token);

        // Create KeyVaultManagementClient to retrieve the Key Vault details.
        // has to be disposable
        using (var keyVaultManagementClient = new KeyVaultManagementClient(credentials)
               {
                   SubscriptionId = _configuration["SUBSCRIPTION_ID"]
               })
        {
            // Retrieve the Key Vault
            Vault vault = await keyVaultManagementClient.Vaults.GetAsync(
                _configuration["RESOURCE_GROUP_NAME"], 
                _configuration["KV_RESOURCE_NAME"]
            );

            if (vault == null)
            {
                // Return false if the vault is not found.
                return false;
            }
        
            // Return true if EnablePurgeProtection is true.
            return vault.Properties.EnablePurgeProtection.HasValue &&
                   vault.Properties.EnablePurgeProtection.Value;
        }
    }

    
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
        var kek = await _keyVaultService.GenerateKekAsync(kekId);
        
        // When I ask to get the public key as PEM
        var gotPem = await _keyVaultService.DownloadPublicKekAsPemAsync(kekId);
        
        // Then the PEM should be the same as the one we generated
        var wantPem = kek.Key.ToRSA().ExportRSAPublicKeyPem();
        Assert.That(gotPem.PemString, Is.EqualTo(wantPem));
        
    }

    [Test]
    public async Task ShouldAbleToDelteAKey()
    {
        // Given a Key Encryption Key 
        var kekName = "kaj";
        var key = await _keyVaultService.GenerateKekAsync(kekName);
        
        // When I ask to delete it
        var delOp = await _keyVaultService.DeleteKeyAsync(kekName);
        delOp.WaitForCompletionAsync();
        
        // Then it should be deleted
        Assert.That(delOp.HasCompleted, Is.True);
        // and the key should be the same
        Assert.That(delOp.Value.Id, Is.EqualTo(key.Id));
    }
    [Test]
    public async Task ShouldAbleToPurgeADeletedKey()
    {
        // Given a Key Encryption Key
        var kekName = "kaj";
        await _keyVaultService.GenerateKekAsync(kekName);
        
        // Which I delete
        var delOp = await _keyVaultService.DeleteKeyAsync(kekName);
        delOp.WaitForCompletionAsync();
        
        // When I ask to purge it
        var res = await _keyVaultService.PurgeDeletedKeyAsync(kekName);
        Console.WriteLine(res);
        
        if (await DoesKeyVaultHavePurgeProtectionAsync())
        {
            Assert.That(res.Status, Is.EqualTo(204));
        }
        
        
    }
}