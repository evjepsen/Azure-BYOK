using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Infrastructure.Interfaces;
using Microsoft.IdentityModel.Tokens;
using Infrastructure.Helpers;
using Infrastructure.Models;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public class TokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IOptions<JwtOptions> jwtOptions, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TokenService>();
        _jwtOptions = jwtOptions.Value;
    }
    
    public KeyTransferBlob CreateKeyTransferBlob(byte[] cipherText, string kekId)
    {
        _logger.LogInformation("Creating key transfer blob");
        var keyTransferBlob = new KeyTransferBlob
        {
            SchemaVersion = "1.0.0",
            Header = new HeaderObject
            {
                Kid = kekId, // The id of the KEK
                Alg = "dir",
                Enc = "CKM_RSA_AES_KEY_WRAP"
            },
            Ciphertext = Base64UrlEncoder.Encode(cipherText),
            Generator = "BYOK v1.0; Azure Key Vault"
        };

        return keyTransferBlob;
    }

    public UploadKeyRequestBody CreateBodyForRequest(KeyTransferBlob transferBlob)
    {
        // Encode the transfer blob in bytes
        _logger.LogInformation("Serializing key transfer blob into Json");
        var serializedKeyTransferBlob = TokenHelper.SerializeJsonObject(transferBlob);
        var bytes = Encoding.UTF8.GetBytes(serializedKeyTransferBlob);
        var transferBlobBase64Encoded = Convert.ToBase64String(bytes);
        
        // Create the json object
        _logger.LogInformation("Creating request body for key upload");
        var keyRequestBody = new UploadKeyRequestBody
        {
            Key = new CustomJwk
            {
                Kty = "RSA-HSM",
                KeyOps = ["decrypt", "encrypt"],
                KeyHsm = transferBlobBase64Encoded
            },
            Attributes = new CustomJwkAttributes
            {
                Enabled = true
            }
        };
        
        return keyRequestBody;
    }

    public string GenerateAccessToken(List<Claim> claims)
    {
        _logger.LogInformation("Generating JWT access token");
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _jwtOptions.Secret)
        );
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: credentials
        );
        
        return jwtTokenHandler.WriteToken(token);
    }
}