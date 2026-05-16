namespace RC.HyRe.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required string Token { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    
    public bool IsRevoked => RevokedAt != null;
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
