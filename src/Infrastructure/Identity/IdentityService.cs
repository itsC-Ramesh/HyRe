using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace RC.HyRe.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService,
        IJwtTokenService jwtTokenService,
        IApplicationDbContext context,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.UserName;
    }

    public async Task<string?> GetUserEmailAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.Email;
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName,
        };

        var result = await _userManager.CreateAsync(user, password);

        return (result.ToApplicationResult(), user.Id);
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        var result = await _authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    public async Task<Result> DeleteUserAsync(ApplicationUser user)
    {
        var result = await _userManager.DeleteAsync(user);

        return result.ToApplicationResult();
    }
    public async Task<AuthResult> AuthenticateAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        
        if (user == null || !user.IsActive)
        {
            return new AuthResult(false, null, null, null, ["Invalid credentials."]);
        }

        var result = await _userManager.CheckPasswordAsync(user, password);

        if (!result)
        {
            return new AuthResult(false, null, null, null, ["Invalid credentials."]);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = GenerateJwtToken(user, roles);
        
        var refreshToken = GenerateRefreshToken(user.Id);
        _context.RefreshTokens.Add(refreshToken);
        
        return new AuthResult(true, accessToken, refreshToken.Token, refreshToken.ExpiresAt, []);
    }

    public async Task<AuthResult> RefreshTokenAsync(string accessToken, string refreshToken)
    {
        var principal = _jwtTokenService.ValidateExpiredToken(accessToken);
        if (principal == null)
            return new AuthResult(false, null, null, null, ["Invalid access token."]);

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return new AuthResult(false, null, null, null, ["Invalid access token."]);

        var storedRefreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (storedRefreshToken == null)
            return new AuthResult(false, null, null, null, ["Invalid refresh token."]);

        if (storedRefreshToken.UserId != userId)
            return new AuthResult(false, null, null, null, ["Invalid refresh token."]);

        if (storedRefreshToken.IsRevoked)
        {
            // Token reuse detected. Revoke all tokens for this user.
            var allUserTokens = await _context.RefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAt == null)
                .ToListAsync();
            
            foreach (var token in allUserTokens)
            {
                token.RevokedAt = _timeProvider.GetUtcNow();
            }
            return new AuthResult(false, null, null, null, ["Invalid refresh token. Potential reuse detected."]);
        }

        if (storedRefreshToken.IsExpired)
            return new AuthResult(false, null, null, null, ["Refresh token expired."]);

        storedRefreshToken.RevokedAt = _timeProvider.GetUtcNow();
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            return new AuthResult(false, null, null, null, ["User no longer valid."]);

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = GenerateJwtToken(user, roles);
        var newRefreshToken = GenerateRefreshToken(userId);

        storedRefreshToken.ReplacedByToken = newRefreshToken.Token;
        _context.RefreshTokens.Add(newRefreshToken);

        return new AuthResult(true, newAccessToken, newRefreshToken.Token, newRefreshToken.ExpiresAt, []);
    }

    public async Task<Result> RevokeRefreshTokenAsync(string refreshToken)
    {
        var storedRefreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (storedRefreshToken == null)
            return Result.Failure(["Invalid refresh token."]);

        if (!storedRefreshToken.IsRevoked)
        {
            storedRefreshToken.RevokedAt = _timeProvider.GetUtcNow();
        }

        return Result.Success();
    }

    private RC.HyRe.Domain.Entities.RefreshToken GenerateRefreshToken(string userId)
    {
        return new RC.HyRe.Domain.Entities.RefreshToken
        {
            Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)),
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow(),
            ExpiresAt = _timeProvider.GetUtcNow().AddDays(7)
        };
    }

    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return new List<string>();
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<Result> AssignRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Result.Failure(["User not found."]);

        var result = await _userManager.AddToRoleAsync(user, role);
        return result.ToApplicationResult();
    }

    public async Task<Result> RemoveRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Result.Failure(["User not found."]);

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        return result.ToApplicationResult();
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        return _jwtTokenService.GenerateAccessToken(user.Id, user.Email!, roles);
    }
}
