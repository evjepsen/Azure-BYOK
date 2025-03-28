using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Test.TestHelpers;

namespace Test;

public class TestTokenService
{
    private ITokenService _tokenService;
    private IConfiguration _configuration;

    [SetUp]
    public void Setup()
    {
        TestHelper.CreateTestConfiguration();
        var configuration = TestHelper.CreateTestConfiguration();
        _tokenService = new TokenService(TestHelper.CreateJwtOptions(configuration));
        _configuration = TestHelper.CreateTestConfiguration();
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
        Assert.IsNotNull(requestBody.Key.KeyOps);
        Assert.True(requestBody.Key.KeyOps.Contains("encrypt"));
        Assert.True(requestBody.Key.KeyOps.Contains("decrypt"));
        Assert.That(requestBody.Key.Kty, Is.EqualTo("RSA-HSM"));
        Assert.That(requestBody.Attributes.Enabled, Is.True);
    }
    
    [Test]
    public void ShouldGenerateAccessToken()
    {
        // Given a token service and some claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "id" ),
            new(ClaimTypes.Email, "john.doe@gmail.com"),
            new(ClaimTypes.Name, "John Doe"),
            new("provider", "Google"),
        };
        
        // When I ask for an access token
        var accessToken = _tokenService.GenerateAccessToken(claims);
        
        // Then it should be created
        Assert.IsNotEmpty(accessToken);
        
        // And well-formed
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);
        
        Assert.That(jwt.Issuer, Is.EqualTo(_configuration["Jwt:Issuer"]));
        Assert.That(jwt.Audiences, Does.Contain(_configuration["Jwt:Audience"]));
        
        var gotClaims = jwt.Claims.ToList();
        Assert.That(gotClaims, Has.Count.EqualTo(7));
        Assert.That(gotClaims, Has.Some.Matches<Claim>(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "id"));
        Assert.That(gotClaims, Has.Some.Matches<Claim>(c => c.Type == ClaimTypes.Email && c.Value == "john.doe@gmail.com"));
        Assert.That(gotClaims, Has.Some.Matches<Claim>(c => c.Type == ClaimTypes.Name && c.Value == "John Doe"));
        Assert.That(gotClaims, Has.Some.Matches<Claim>(c => c.Type == "provider" && c.Value == "Google"));
    }
    
    
}