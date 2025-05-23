using Azure.Security.KeyVault.Keys;
using FakeHSM;
using FakeHSM.Interfaces;
using Infrastructure;
using Infrastructure.Factories;
using Infrastructure.Interfaces;
using Infrastructure.TransferBlobStrategies;
using Microsoft.Extensions.Logging.Abstractions;
using Test.TestHelpers;

namespace Test.Infrastructure;

[TestFixture]
[TestOf(typeof(KeyVaultService))]
public class TestKeyVaultService
{
    private ITokenService _tokenService;
    private IKeyVaultService _keyVaultService;
    private IKeyVaultManagementService _keyVaultManagementService;
    private readonly List<string> _createdKeys = [];
    private IFakeHsm _hsm;

    [SetUp]
    public void Setup()
    {
        TestHelper.CreateTestConfiguration();
        IHttpClientFactory httpClientFactory = new FakeHttpClientFactory();
        ICryptographyClientFactory cryptographyClientFactory = new CryptographyClientFactory(httpClientFactory);
        
        var configuration = TestHelper.CreateTestConfiguration();
        _tokenService = new TokenService(new NullLoggerFactory());
        
        var applicationOptions = TestHelper.CreateApplicationOptions(configuration);
        
        var certificateCache = new CertificateCache(new NullLoggerFactory(), applicationOptions);
        var signatureService = new SignatureService(certificateCache,
            httpClientFactory,
            applicationOptions,
            cryptographyClientFactory,
            new NullLoggerFactory());        
        _keyVaultService = new KeyVaultService(_tokenService, 
            signatureService, 
            httpClientFactory, 
            applicationOptions, 
            new NullLoggerFactory());
        _keyVaultManagementService = new KeyVaultManagementService(applicationOptions, new NullLoggerFactory());
        
        _hsm = new FakeHsm(_tokenService);
    }

    [Test]
    public async Task ShouldBePossibleToCreateAKek()
    {
        // Given a key vault service
        // When I ask to create a key encryption key
        var kekName = $"KEK-{Guid.NewGuid()}";
        _createdKeys.Add(kekName);
        
        var kekSignedResponse = await _keyVaultService.GenerateKekAsync(kekName);
        var kek = kekSignedResponse.Kek;
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
        // Given a Key Encryption Key and encrypted key
        var kekName = $"KEK-{Guid.NewGuid()}";
        _createdKeys.Add(kekName);
        
        var kekSignedResponse = await _keyVaultService.GenerateKekAsync(kekName);
        var kek = kekSignedResponse.Kek;
        
        var encryptedKey = _hsm.EncryptPrivateKeyForUpload(kek.Key.ToRSA());
        
        // When is ask to upload it
        var newKeyName = $"customer-KEY-{Guid.NewGuid()}";
        _createdKeys.Add(newKeyName);

        var transferBlobStrategy = new EncryptedKeyTransferBlobStrategy(kek.Id.ToString(), encryptedKey, _tokenService);

        var kvRes = await _keyVaultService.UploadKey(newKeyName, transferBlobStrategy, ["encrypt", "decrypt"]);
        
        // Then it should be successful
        Assert.That(kvRes.Attributes.Enabled, Is.True);
    }

    [Test]
    public async Task ShouldBeAbleToDeleteAKek()
    {
        // Given a Key Encryption Key 
        var kekName = $"Random-delete-{Guid.NewGuid()}";
        var kekSignedResponse = await _keyVaultService.GenerateKekAsync(kekName);
        var kek = kekSignedResponse.Kek;
        
        // When I ask to delete it
        var delOp = await _keyVaultService.DeleteKeyAsync(kekName);
        
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
        await _keyVaultService.DeleteKeyAsync(kekName);
        
        // When I ask to purge it
        // Then it should be purged if the key vault has purge protection
        if (!_keyVaultManagementService.DoesKeyVaultHavePurgeProtection())
        {
            var purgeKekOperation = await _keyVaultService.PurgeDeletedKeyAsync(kekName);
            Assert.That(purgeKekOperation.Status, Is.EqualTo(204));
        }
        // Otherwise, purging is not possible and an exception should be thrown 
        else
        {
            Assert.ThrowsAsync<Azure.RequestFailedException>(async () => await _keyVaultService.PurgeDeletedKeyAsync(kekName));
        }
    }
    
    [Test]
    public async Task ShouldBeAbleToRecoverADeletedKey()
    {
        // Given a Key Encryption Key
        var keyName = $"Random-recover-{Guid.NewGuid()}";
        _createdKeys.Add(keyName);
        await _keyVaultService.GenerateKekAsync(keyName);
        
        // Which I delete
        await _keyVaultService.DeleteKeyAsync(keyName);
        
        // When I ask to recover it
        var recoverOp = await _keyVaultService.RecoverDeletedKeyAsync(keyName);
        
        // Then it should be recovered and the operation should be completed
        Assert.That(recoverOp.HasCompleted, Is.EqualTo(true));
    }

    [Test]
    public async Task ShouldBePossibleToUploadAKeyBlobSpecifiedByAUser()
    {
        // Given a Key Encryption Key and transfer blob
        var kekName = $"KEK-{Guid.NewGuid()}";
        _createdKeys.Add(kekName);
        
        var kekSignedResponse = await _keyVaultService.GenerateKekAsync(kekName);
        var kek = kekSignedResponse.Kek;
        var transferBlob = _hsm.GenerateBlobForUpload(kek.Key.ToRSA(), kek.Id.ToString());
        
        // When is ask to upload it
        var newKeyName = $"customer-KEY-{Guid.NewGuid()}";
        _createdKeys.Add(newKeyName);
        
        var transferBlobStrategy = new SpecifiedTransferBlobStrategy(transferBlob);
        
        var kvRes = await _keyVaultService.UploadKey(newKeyName, transferBlobStrategy, ["encrypt", "decrypt", "sign", "verify", "wrapKey", "unwrapKey"]);
        
        // Then it should be successful
        Assert.That(kvRes.Attributes.Enabled, Is.True);
    }

    [Test]
    public async Task ShouldExistingKeyExist()
    {
        // Given a key in the key vault
        var keyName = $"key-{Guid.NewGuid()}";
        _createdKeys.Add(keyName);
        
        await _keyVaultService.GenerateKekAsync(keyName);
        
        // When I check whether it exists
        var keyExists = await _keyVaultService.CheckIfKeyExistsAsync(keyName);
        // Then it should
        Assert.That(keyExists, Is.True);
    }
    
    [Test]
    public async Task ShouldKeyThatIsNotAddedNotExist()
    {
        // Given a key in the key vault
        var keyName = $"key-{Guid.NewGuid()}";
        
        // When I check whether it exists
        var keyExists = await _keyVaultService.CheckIfKeyExistsAsync(keyName);

        // Then it should
        Assert.That(keyExists, Is.False);
    }

    [Test]
    public void ShouldAllowedOperationsBeValid()
    {
        // Given a list of key operations
        string[] operations = ["encrypt", "decrypt", "sign", "verify", "wrapKey", "unwrapKey"];
        // When I ask whether they are valid
        var result = _keyVaultService.ValidateKeyOperations(operations);
        // Then they should be valid
        Assert.That(result.IsValid, Is.True);
    } 
    
    [Test]
    public void ShouldNotAllowedOperationsBeInvalid()
    {
        // Given a list of key operations
        string[] operations = ["encrypt", "decrypt", "sign", "abc"];
        // When I ask whether they are valid
        var result = _keyVaultService.ValidateKeyOperations(operations);
        // Then they should be valid
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Invalid key operations detected: abc"));
    }

    [Test]
    public async Task ShouldReturnValidPemOnKnownKek()
    {
        // Given that there is a known key in the key vault
        var keyName = $"key-{Guid.NewGuid()}";
        _createdKeys.Add(keyName);
        
        await _keyVaultService.GenerateKekAsync(keyName);
        
        // When I ask for it's PEM
        var pem = await _keyVaultService.GetPemOfKey(keyName);
        
        // Then it should return the PEM
        Assert.That(pem, Is.Not.Empty);
        Assert.That(pem, Does.StartWith("-----BEGIN RSA PUBLIC KEY-----"));
        Assert.That(pem, Does.EndWith("-----END RSA PUBLIC KEY-----"));
    }
    
    
    [OneTimeTearDown]
    public async Task TearDown()
    {
        foreach (var key in _createdKeys)
        {
            await _keyVaultService.DeleteKeyAsync(key);
        }
    }
}