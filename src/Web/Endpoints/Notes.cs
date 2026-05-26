using MediatR;
using RC.HyRe.Application.Notes.Commands.AddNote;
using RC.HyRe.Application.Notes.Commands.DeleteNote;
using RC.HyRe.Application.Notes.Commands.UpdateNote;
using RC.HyRe.Application.Notes.Queries.GetNotes;
using RC.HyRe.Web.Infrastructure;

namespace RC.HyRe.Web.Endpoints;

public class NotesEndpoints : IEndpointGroup
{
    public static string RoutePrefix => "/api/v1/notes";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetNotes).WithName("GetNotes");
        groupBuilder.MapPost("", AddNote).WithName("AddNote");
        groupBuilder.MapPut("{id:guid}", UpdateNote).WithName("UpdateNote");
        groupBuilder.MapDelete("{id:guid}", DeleteNote).WithName("DeleteNote");
    }

    private static async Task<IResult> GetNotes(
        string entityType, Guid entityId, int pageNumber, int pageSize, ISender sender, CancellationToken ct)
    {
        // default parameter logic is not fully supported in MapGet query strings directly by some older versions, but 9+ handles it.
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        var result = await sender.Send(new GetNotesQuery(entityType, entityId, pageNumber, pageSize), ct);
        return result.Succeeded
            ? TypedResults.Ok(ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("GET_NOTES_FAILED", "Failed to get notes", result.Errors));
    }

    private static async Task<IResult> AddNote(
        AddNoteCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Succeeded
            ? TypedResults.Created($"/api/v1/notes?entityType={command.EntityType}&entityId={command.EntityId}", ApiResponse.Ok(result.Value))
            : TypedResults.BadRequest(ApiResponse.Fail("CREATE_NOTE_FAILED", "Failed to create note", result.Errors));
    }

    private static async Task<IResult> UpdateNote(
        Guid id, UpdateNoteCommand command, ISender sender, CancellationToken ct)
    {
        if (id != command.NoteId)
            return TypedResults.BadRequest(ApiResponse.Fail("ID_MISMATCH", "Route id and command id do not match."));

        var result = await sender.Send(command, ct);
        return result.Succeeded
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(ApiResponse.Fail("UPDATE_NOTE_FAILED", "Failed to update note", result.Errors));
    }

    private static async Task<IResult> DeleteNote(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteNoteCommand(id), ct);
        return result.Succeeded
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(ApiResponse.Fail("DELETE_NOTE_FAILED", "Failed to delete note", result.Errors));
    }
}
