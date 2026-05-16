using RC.HyRe.Application.Common.Interfaces;
using RC.HyRe.Application.Common.Security;

namespace RC.HyRe.Application.Auth.Queries;

[Authorize]
public record GetCurrentUserQuery : IRequest<CurrentUserDto>;

public record CurrentUserDto(string Id, string Email, IList<string> Roles);

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly IUser _user;

    public GetCurrentUserQueryHandler(IUser user)
    {
        _user = user;
    }

    public Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CurrentUserDto(
            _user.Id ?? string.Empty, 
            _user.Email ?? string.Empty, 
            _user.Roles ?? new List<string>()));
    }
}
