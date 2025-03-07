using System.Text.Json;
using System.Text.Json.Nodes;
using Infrastructure;
using Infrastructure.Interfaces;
using Infrastructure.Models;
using Microsoft.IdentityModel.Tokens;

namespace Test;

public class TestTokenService
{
    private ITokenService _tokenService;
    
    [SetUp]
    public void Setup()
    {
        _tokenService = new TokenService();
    }
    
    [Test]
    public void ShouldKeyTransferBlobValuesSpecification()
    {
        // Proper KeyTranferBlob:
        //{
        //   "schema_version": "1.0.0",
        //   "header":
        //   {
        //     "kid": "<key identifier of the KEK>",
        //     "alg": "dir",
        //     "enc": "CKM_RSA_AES_KEY_WRAP"
        //   },
        //   "ciphertext":"BASE64URL(<ciphertext contents>)",
        //   "generator": "BYOK tool name and version; source HSM name and firmware version"
        // }
        // 
        
        // Given a dummy KeyTransferBlob
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
}