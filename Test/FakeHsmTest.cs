using FakeHSM.Interfaces;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Test.TestHelpers;
using FakeHsm = FakeHSM.FakeHsm;

namespace Test;

[TestFixture]
[TestOf(typeof(FakeHsm))]
public class FakeHsmTest
{
    private IFakeHsm _fakeHsm;
    private TokenService _tokenService;
    private readonly List<string> _createdKeys = [];
    private KeyVaultService _keyVaultService;

    [SetUp]
    public void Setup()
    {
        _tokenService = new TokenService(new NullLoggerFactory());
        var signatureService = new SignatureService(new CertificateCache(), new NullLoggerFactory());
        _fakeHsm = new FakeHsm(_tokenService, signatureService);
        var configuration = TestHelper.CreateTestConfiguration();
        IHttpClientFactory httpClientFactory = new FakeHttpClientFactory();
        _keyVaultService = new KeyVaultService(_tokenService, 
            httpClientFactory, 
            TestHelper.CreateApplicationOptions(configuration), 
            new NullLoggerFactory());
    }
    
    [Test]
    public async Task ShouldBlobBeGenerated()
    {
        
        var kekName = $"KEK-{Guid.NewGuid()}";
        _createdKeys.Add(kekName);
        
        var kek = await _keyVaultService.GenerateKekAsync(kekName);
        // Given a key vault service and Key Encryption Key 
        
        // When I ask to generate a blob
        var transferBlob = _fakeHsm.EncryptPrivateKeyForUpload(kek.Key.ToRSA());
        
        // Then it should be successful
        Assert.That(transferBlob, Is.Not.Null);
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