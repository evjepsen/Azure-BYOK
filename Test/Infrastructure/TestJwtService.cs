using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Infrastructure;
using Infrastructure.Interfaces;
using Infrastructure.Options;
using Test.TestHelpers;

namespace Test.Infrastructure;

[TestFixture]
[TestOf(typeof(JwtService))]
public class TestJwtService
{
    private JwtOptions _jwtOptions;
    private IJwtService _jwtService;

    [SetUp]
    public void Setup()
    {
        TestHelper.CreateTestConfiguration();
        var configuration = TestHelper.CreateTestConfiguration();
        var jwtOptions = TestHelper.CreateJwtOptions(configuration);
        _jwtOptions = jwtOptions.Value;
        _jwtService = new JwtService(jwtOptions);

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
        var accessToken = _jwtService.GenerateAccessToken(claims);
        
        // Then it should be created
        Assert.IsNotEmpty(accessToken);
        
        // And well-formed
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);
        
        Assert.That(jwt.Issuer, Is.EqualTo(_jwtOptions.Issuer));
        Assert.That(jwt.Audiences, Does.Contain(_jwtOptions.Audience));
        
        var gotClaims = jwt.Claims.ToList();
        Assert.That(gotClaims, Has.Some.Matches<Claim>(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "id"));
        Assert.That(gotClaims, Has.Some.Matches<Claim>(c => c.Type == ClaimTypes.Email && c.Value == "john.doe@gmail.com"));
        Assert.That(gotClaims, Has.Some.Matches<Claim>(c => c.Type == ClaimTypes.Name && c.Value == "John Doe"));
        Assert.That(gotClaims, Has.Some.Matches<Claim>(c => c.Type == "provider" && c.Value == "Google"));
    }
}