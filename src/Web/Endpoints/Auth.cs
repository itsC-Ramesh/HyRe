using RC.HyRe.Application.Auth.Commands;
using RC.HyRe.Application.Auth.Queries;
using RC.HyRe.Application.Common.Models;
using RC.HyRe.Web.Infrastructure;

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

    public static async Task<IResult> Login(ISender sender, LoginCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result))
            : TypedResults.BadRequest(ApiResponse.Fail("INVALID_CREDENTIALS", "Login failed.", result.Errors));
    }

    public static async Task<IResult> Register(ISender sender, RegisterCandidateCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok())
            : TypedResults.BadRequest(ApiResponse.Fail("REGISTRATION_FAILED", "Registration failed.", result.Errors));
    }

    public static async Task<IResult> RefreshToken(ISender sender, RefreshTokenCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result))
            : TypedResults.BadRequest(ApiResponse.Fail("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired.", result.Errors));
    }

    public static async Task<IResult> Logout(ISender sender, LogoutCommand command)
    {
        var result = await sender.Send(command);
        return result.Succeeded
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(ApiResponse.Fail("LOGOUT_FAILED", "Logout failed.", result.Errors));
    }

    public static async Task<IResult> GetMe(ISender sender)
    {
        var result = await sender.Send(new GetCurrentUserQuery());
        return result != null
            ? TypedResults.Ok(ApiResponse.Ok(result))
            : TypedResults.Unauthorized();
    }
}
