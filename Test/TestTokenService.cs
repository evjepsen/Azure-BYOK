using Infrastructure;
using Infrastructure.Interfaces;

namespace Test;

public class TestTokenService
{
    private ITokenService _tokenService;
    
    [SetUp]
    public void Setup()
    {
        _tokenService = new TokenService();
    }
}