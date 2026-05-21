using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Templates.Queries;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Infrastructure.Services;

public partial class TemplateService : ITemplateService
{
    private readonly IApplicationDbContext _context;

    public TemplateService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> CreateAsync(
        string name,
        TemplateCategory category,
        string subject,
        string body,
        CancellationToken ct = default)
    {
        try
        {
            var template = new Template
            {
                Name = name,
                Category = category,
                Subject = subject,
                Body = body,
            };

            _context.Templates.Add(template);
            await _context.SaveChangesAsync(ct);

            return Result.Success(template.Id);
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>($"Failed to create template: {ex.Message}");
        }
    }

    public async Task<Result> UpdateAsync(
        Guid id,
        string subject,
        string body,
        CancellationToken ct = default)
    {
        try
        {
            var template = await _context.Templates
                .Include(t => t.Versions)
                .FirstOrDefaultAsync(t => t.Id == id, ct);

            if (template is null)
                return Result.Failure("Template not found.");

            // Snapshot current state into a version record
            var version = new TemplateVersion
            {
                TemplateId = template.Id,
                Version = template.Version,
                Subject = template.Subject,
                Body = template.Body,
            };

            _context.TemplateVersions.Add(version);

            // Update the template
            template.Subject = subject;
            template.Body = body;
            template.Version += 1;

            await _context.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to update template: {ex.Message}");
        }
    }

    public async Task<Result<TemplateDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var dto = await _context.Templates
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new TemplateDto(
                    t.Id,
                    t.Name,
                    t.Category,
                    t.Subject,
                    t.Body,
                    t.Version,
                    t.IsActive,
                    t.IsBuiltIn,
                    t.Created))
                .FirstOrDefaultAsync(ct);

            if (dto is null)
                return Result.Failure<TemplateDto>("Template not found.");

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            return Result.Failure<TemplateDto>($"Failed to get template: {ex.Message}");
        }
    }

    public async Task<Result<PaginatedList<TemplateDto>>> GetPagedAsync(
        TemplateCategory? category,
        int page,
        int limit,
        CancellationToken ct = default)
    {
        try
        {
            var query = _context.Templates
                .AsNoTracking()
                .AsQueryable();

            if (category.HasValue)
                query = query.Where(t => t.Category == category.Value);

            var projected = query
                .OrderByDescending(t => t.Created)
                .Select(t => new TemplateDto(
                    t.Id,
                    t.Name,
                    t.Category,
                    t.Subject,
                    t.Body,
                    t.Version,
                    t.IsActive,
                    t.IsBuiltIn,
                    t.Created));

            var list = await PaginatedList<TemplateDto>.CreateAsync(projected, page, limit, ct);

            return Result.Success(list);
        }
        catch (Exception ex)
        {
            return Result.Failure<PaginatedList<TemplateDto>>($"Failed to get templates: {ex.Message}");
        }
    }

    public async Task<Result<string>> RenderAsync(
        Guid templateId,
        Dictionary<string, string> variables,
        CancellationToken ct = default)
    {
        try
        {
            var template = await _context.Templates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == templateId, ct);

            if (template is null)
                return Result.Failure<string>("Template not found.");

            var rendered = VariablePattern().Replace(template.Body, match =>
            {
                var key = match.Groups[1].Value;
                return variables.TryGetValue(key, out var value) ? value : match.Value;
            });

            return Result.Success(rendered);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Failed to render template: {ex.Message}");
        }
    }

    public async Task<Result<string>> RenderByCategoryAsync(
        TemplateCategory category,
        Dictionary<string, string> variables,
        CancellationToken ct = default)
    {
        try
        {
            var template = await _context.Templates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Category == category && t.IsActive, ct);

            if (template is null)
                return Result.Failure<string>($"No active template found for category '{category}'.");

            return await RenderAsync(template.Id, variables, ct);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Failed to render template by category: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var template = await _context.Templates
                .FirstOrDefaultAsync(t => t.Id == id, ct);

            if (template is null)
                return Result.Failure("Template not found.");

            if (template.IsBuiltIn)
                return Result.Failure("Built-in templates cannot be deleted.");

            _context.Templates.Remove(template);
            await _context.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete template: {ex.Message}");
        }
    }

    public async Task<Result<string>> PreviewAsync(Guid templateId, CancellationToken ct = default)
    {
        try
        {
            var template = await _context.Templates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == templateId, ct);

            if (template is null)
                return Result.Failure<string>("Template not found.");

            var preview = VariablePattern().Replace(template.Body, match =>
            {
                var varName = match.Groups[1].Value;
                return $"[{varName}]";
            });

            return Result.Success(preview);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Failed to preview template: {ex.Message}");
        }
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex VariablePattern();
}
