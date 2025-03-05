using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;
using Infrastructure;
using Infrastructure.Interfaces;
using Test.TestHelpers;

namespace Test;

public class TestKeyVaultService
{
    private ITokenService _tokenService;
    private IKeyVaultService _keyVaultService;
    
    [SetUp]
    public void Setup()
    {
        _tokenService = new TokenService();
        _keyVaultService = new KeyVaultService(_tokenService);
    }

    [Test]
    public void ShouldBePossibleToEncryptKeyWithKekAndUpload()
    {
        var testKey = TestHelper.CreateTestKey();

        var testKeyBytes = Encoding.UTF8.GetBytes(testKey.ToString());
        
        var kek = _keyVaultService.GenerateKek("test-kek").Value;

        var cryptoClient = new CryptographyClient(kek.Id, new DefaultAzureCredential());

        var res = cryptoClient.Encrypt(EncryptionAlgorithm.RsaOaep, testKeyBytes).Ciphertext;

        _keyVaultService.ImportKey("New key", res, kek.Id.ToString());

    } 
}