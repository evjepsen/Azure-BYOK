using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.IdentityModel.Tokens;
using Test.TestHelpers;

namespace Test;

public class TestTokenService
{
    private ITokenService _tokenService;
    
    [SetUp]
    public void Setup()
    {
        TestHelper.CreateTestConfiguration();
        _tokenService = new TokenService();
    }
    
    [Test]
    public void ShouldKeyTransferBlobValuesSpecification()
    {
        // Given a token service
        // When I ask for a KeyTransferBlob
        var keyTransferBlob = _tokenService.CreateKeyTransferBlob([], "KEK_ID");
        
        // Then the KeyTransferBlob should be well-formed
        // Which means the schema version should be 1.0.0
        Assert.That(keyTransferBlob.SchemaVersion, Is.EqualTo("1.0.0"));
        
        // and the header should be well-formed
        Assert.That(keyTransferBlob.Header.Kid, Is.EqualTo("KEK_ID"));
        Assert.That(keyTransferBlob.Header.Alg, Is.EqualTo("dir"));
        Assert.That(keyTransferBlob.Header.Enc, Is.EqualTo("CKM_RSA_AES_KEY_WRAP"));
        
        // and the ciphertext should be well-formed
        Assert.That(keyTransferBlob.Ciphertext, Is.EqualTo(Base64UrlEncoder.Encode("")));
        Assert.That(keyTransferBlob.Generator, Is.EqualTo("BYOK v1.0; Azure Key Vault"));
    }

    [Test]
    public void ShouldRequestBodyForRsaKeyUploadHaveTheCorrectValues()
    {
        // Given a token service and a key transfer blob
        var keyTransferBlob = _tokenService.CreateKeyTransferBlob([], "KEK_ID");
        // When create the request body for an upload request
        var requestBody = _tokenService.CreateBodyForRequest(keyTransferBlob);
        // Then it should be well-formed 
        Assert.IsNotEmpty(requestBody.Key.KeyHsm);
        Assert.True(requestBody.Key.KeyOps.Contains("encrypt"));
        Assert.True(requestBody.Key.KeyOps.Contains("decrypt"));
        Assert.That(requestBody.Key.Kty, Is.EqualTo("RSA-HSM"));
        Assert.That(requestBody.Attributes.Enabled, Is.True);

    }
    
    
}