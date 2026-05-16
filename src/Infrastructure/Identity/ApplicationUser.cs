using Microsoft.AspNetCore.Identity;

using RC.HyRe.Domain.Entities;

namespace RC.HyRe.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
