using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Templates.Queries;

public record TemplateDto(
    Guid Id,
    string Name,
    TemplateCategory Category,
    string Subject,
    string Body,
    int Version,
    bool IsActive,
    bool IsBuiltIn,
    DateTimeOffset Created);
