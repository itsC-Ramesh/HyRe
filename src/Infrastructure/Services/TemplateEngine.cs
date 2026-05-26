using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Infrastructure.Services;

public class TemplateEngine(IApplicationDbContext db) : ITemplateEngine
{
    public async Task<TemplateResult> RenderAsync(TemplateCategory category, Dictionary<string, string> variables, CancellationToken ct = default)
    {
        var template = await db.Templates
            .AsNoTracking()
            .Where(t => t.Category == category && t.IsActive)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(ct);

        if (template == null)
            throw new InvalidOperationException($"No active template found for category {category}");

        var subject = ReplaceVariables(template.Subject, variables);
        var body = ReplaceVariables(template.Body, variables);

        return new TemplateResult(subject, body);
    }

    private static string ReplaceVariables(string text, Dictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        foreach (var kvp in variables)
        {
            text = text.Replace("{{" + kvp.Key + "}}", kvp.Value);
        }

        return text;
    }
}
