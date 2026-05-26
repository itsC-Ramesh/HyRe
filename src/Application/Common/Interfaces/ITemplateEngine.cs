using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Common.Interfaces;

public interface ITemplateEngine
{
    Task<TemplateResult> RenderAsync(TemplateCategory category, Dictionary<string, string> variables, CancellationToken ct = default);
}

public record TemplateResult(string Subject, string Body);
