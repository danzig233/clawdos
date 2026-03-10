using Clawdos.Models;
using Clawdos.Services;

namespace Clawdos.Endpoints;

public static class FileSystemEndpoints
{
    public static void MapFileSystemEndpoints(this WebApplication app)
    {
        // ── List
        app.MapGet("/v1/fs/list", (int rootId, string? path, FileSandboxService fs) =>
        {
            try
            {
                return Results.Ok(fs.List(rootId, path ?? "."));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new ApiError(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Json(new ApiError("Path escapes sandbox"), statusCode: 403);
            }
            catch (DirectoryNotFoundException ex)
            {
                return Results.NotFound(new ApiError(ex.Message));
            }
        });
        // ── Read
        app.MapGet("/v1/fs/read", (int rootId, string path, FileSandboxService fs) =>
        {
            try
            {
                return Results.Ok(fs.Read(rootId, path));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new ApiError(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Json(new ApiError("Path escapes sandbox"), statusCode: 403);
            }
            catch (FileNotFoundException ex)
            {
                return Results.NotFound(new ApiError(ex.Message));
            }
        });
        // ── Write
        app.MapPost("/v1/fs/write", (FsWriteRequest req, FileSandboxService fs) =>
        {
            try
            {
                fs.Write(req.RootId, req.Path, req.Data, req.Overwrite);
                return Results.Ok(new ApiOk(true));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new ApiError(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Json(new ApiError("Path escapes sandbox"), statusCode: 403);
            }
            catch (IOException ex)
            {
                return Results.Conflict(new ApiError(ex.Message));
            }
        });
        // ── Mkdir
        app.MapPost("/v1/fs/mkdir", (FsMkdirRequest req, FileSandboxService fs) =>
        {
            try
            {
                fs.Mkdir(req.RootId, req.Path);
                return Results.Ok(new ApiOk(true));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new ApiError(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Json(new ApiError("Path escapes sandbox"), statusCode: 403);
            }
        });
        // ── Delete
        app.MapPost("/v1/fs/delete", (FsDeleteRequest req, FileSandboxService fs) =>
        {
            try
            {
                fs.Delete(req.RootId, req.Path, req.Recursive);
                return Results.Ok(new ApiOk(true));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new ApiError(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Json(new ApiError("Path escapes sandbox"), statusCode: 403);
            }
            catch (FileNotFoundException ex)
            {
                return Results.NotFound(new ApiError(ex.Message));
            }
            catch (IOException ex)
            {
                return Results.BadRequest(new ApiError(ex.Message));
            }
        });
        // ── Move
        app.MapPost("/v1/fs/move", (FsMoveRequest req, FileSandboxService fs) =>
        {
            try
            {
                fs.MoveEntry(req.RootId, req.From, req.To, req.Overwrite);
                return Results.Ok(new ApiOk(true));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new ApiError(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Json(new ApiError("Path escapes sandbox"), statusCode: 403);
            }
            catch (FileNotFoundException ex)
            {
                return Results.NotFound(new ApiError(ex.Message));
            }
            catch (IOException ex)
            {
                return Results.Conflict(new ApiError(ex.Message));
            }
        });
    }
}
