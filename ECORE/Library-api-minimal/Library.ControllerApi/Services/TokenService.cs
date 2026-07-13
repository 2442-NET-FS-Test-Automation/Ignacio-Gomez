using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

namespace Library.ControllerApi.Services;

public class TokenService : ITokenService
{
    private readonly string _key;
    
    public TokenService(IConfiguration config)
    {
        // We probably want to avoid hardcoding the basis of our key
        // we can always add it to appsettings
    }
}