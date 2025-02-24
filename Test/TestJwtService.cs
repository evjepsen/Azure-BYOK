using System.IdentityModel.Tokens.Jwt;
using Infrastructure;
using Infrastructure.Interfaces;

namespace Test;

public class TestJwtService
{
    private IJwtService _jwtService;
    
    [SetUp]
    public void Setup()
    {
        _jwtService = new JwtService();
    }

    [Test]
    public void ShouldCreateAToken()
    {
        // Given a token
        var token = _jwtService.CreateToken();
        // When I check whether it has an "Aud" field
        var jwtToken = token as JwtSecurityToken;
        var audiences = jwtToken!.Claims
            .Where(c => c.Type == "aud")
            .Select(c => c.Value)
            .ToList();
    
        // Then it should have the expected audience
        Assert.That(audiences, Contains.Item("api://AzureADTokenExchange")); 
    }

}