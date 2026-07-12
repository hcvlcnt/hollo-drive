using backend.Models;
using System.Security.Claims;

namespace backend.Services
{
    public interface ITokenService
    {
        AuthResponse CreateAccessToken(ApplicationUser user);
        ClaimsPrincipal? ValidateToken(string token);
    }
}
