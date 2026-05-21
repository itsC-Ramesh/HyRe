using RC.HyRe.Application.Common.Models;
using RC.HyRe.Application.Templates.Queries;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Application.Common.Interfaces;

public interface ITemplateService
{
    Task<Result<Guid>> CreateAsync(string name, TemplateCategory category, string subject, string body, CancellationToken ct = default);
    Task<Result> UpdateAsync(Guid id, string subject, string body, CancellationToken ct = default);
    Task<Result<TemplateDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PaginatedList<TemplateDto>>> GetPagedAsync(TemplateCategory? category, int page, int limit, CancellationToken ct = default);
    Task<Result<string>> RenderAsync(Guid templateId, Dictionary<string, string> variables, CancellationToken ct = default);
    Task<Result<string>> RenderByCategoryAsync(TemplateCategory category, Dictionary<string, string> variables, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<string>> PreviewAsync(Guid templateId, CancellationToken ct = default);
}
