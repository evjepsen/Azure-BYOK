using FakeHSM.Interfaces;
using Infrastructure;
using Infrastructure.Factories;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Test.TestHelpers;
using FakeHsm = FakeHSM.FakeHsm;

namespace Test.Infrastructure;

[TestFixture]
[TestOf(typeof(FakeHsm))]
public class TestFakeHsm
{
    private IFakeHsm _fakeHsm;
    private TokenService _tokenService;
    private readonly List<string> _createdKeys = [];
    private KeyVaultService _keyVaultService;

    [SetUp]
    public void Setup()
    {
        var configuration = TestHelper.CreateTestConfiguration();
        var applicationOptions = TestHelper.CreateApplicationOptions(configuration);
        IHttpClientFactory httpClientFactory = new FakeHttpClientFactory();
        ICryptographyClientFactory cryptographyClientFactory = new CryptographyClientFactory(httpClientFactory);

        
        _tokenService = new TokenService(new NullLoggerFactory());
        var certificateCache = new CertificateCache(new NullLoggerFactory(), applicationOptions);
        var signatureService = new SignatureService(certificateCache, 
            httpClientFactory, 
            applicationOptions, 
            cryptographyClientFactory,
            new NullLoggerFactory());        
        _fakeHsm = new FakeHsm(_tokenService);
        _keyVaultService = new KeyVaultService(_tokenService, 
            signatureService,
            httpClientFactory, 
            applicationOptions, 
            new NullLoggerFactory());
    }
    
    [Test]
    public async Task ShouldBlobBeGenerated()
    {
        
        var kekName = $"KEK-{Guid.NewGuid()}";
        _createdKeys.Add(kekName);
        
        var kekSignedResponse = await _keyVaultService.GenerateKekAsync(kekName);
        var kek = kekSignedResponse.Kek;
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