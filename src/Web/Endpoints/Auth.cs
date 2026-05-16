using RC.HyRe.Application.Auth.Commands;
using RC.HyRe.Application.Auth.Queries;
using Microsoft.AspNetCore.Http.HttpResults;
using RC.HyRe.Application.Common.Models;

namespace RC.HyRe.Web.Endpoints;

public class Auth : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Login, "login");
        groupBuilder.MapPost(Register, "register");
        groupBuilder.MapPost(RefreshToken, "refresh");
        groupBuilder.MapPost(Logout, "logout");
        groupBuilder.MapGet(GetMe, "me");
    }

    public static async Task<Results<Ok<AuthResult>, BadRequest<string[]>>> Login(ISender sender, LoginCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? TypedResults.Ok(result) : TypedResults.BadRequest(result.Errors);
    }

    public static async Task<Results<Ok<Result>, BadRequest<string[]>>> Register(ISender sender, RegisterCandidateCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? TypedResults.Ok(result) : TypedResults.BadRequest(result.Errors);
    }

    public static async Task<Results<Ok<AuthResult>, BadRequest<string[]>>> RefreshToken(ISender sender, RefreshTokenCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? TypedResults.Ok(result) : TypedResults.BadRequest(result.Errors);
    }

    public static async Task<Results<NoContent, BadRequest<string[]>>> Logout(ISender sender, LogoutCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded ? TypedResults.NoContent() : TypedResults.BadRequest(result.Errors);
    }

    public static async Task<Results<Ok<CurrentUserDto>, UnauthorizedHttpResult>> GetMe(ISender sender)
    {
        var result = await sender.Send(new GetCurrentUserQuery());
        return result != null ? TypedResults.Ok(result) : TypedResults.Unauthorized();
    }
}
