using MediatR;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Application.Tags.Queries.GetTags;

public record GetTagsQuery : IRequest<Result<List<TagDto>>>;

public record TagDto(Guid Id, string Name, string? Color);
