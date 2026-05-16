namespace RC.HyRe.Application.Common.Models;

public record AuthResult(
    bool Succeeded,
    string? AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiresAt,
    string[] Errors);
