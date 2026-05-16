using System.Security.Claims;

namespace RC.HyRe.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(string userId, string email, IList<string> roles);
    ClaimsPrincipal? ValidateExpiredToken(string token);
}
