using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password);

    Task<Result> DeleteUserAsync(string userId);

    // JWT Auth
    Task<AuthResult> AuthenticateAsync(string email, string password);
    Task<AuthResult> RefreshTokenAsync(string accessToken, string refreshToken);
    Task<Result> RevokeRefreshTokenAsync(string refreshToken);
    Task<IList<string>> GetUserRolesAsync(string userId);
    Task<Result> AssignRoleAsync(string userId, string role);
    Task<Result> RemoveRoleAsync(string userId, string role);
}
