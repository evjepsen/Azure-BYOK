using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Interfaces;

public interface IJwtService
{
    public SecurityToken CreateToken();

    public string ConvertTokenToString(SecurityToken token);
}