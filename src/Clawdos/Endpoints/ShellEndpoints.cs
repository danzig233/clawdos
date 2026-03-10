using Clawdos.Models;
using Clawdos.Services;

namespace Clawdos.Endpoints;

public static class ShellEndpoints
{
    public static void MapShellEndpoints(this WebApplication app)
    {
        // ── Execute shell command ─────────────────────
        app.MapPost("/v1/shell/exec", async (
            ShellExecRequest req,
            ShellService shell) =>
        {
            try
            {
                var result = await shell.ExecuteAsync(req);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(
                    new ApiError(ex.Message), statusCode: 403);
            }
            catch (DirectoryNotFoundException ex)
            {
                return Results.NotFound(new ApiError(ex.Message));
            }
            catch (Exception ex)
            {
                return Results.Json(
                    new ApiError(ex.Message), statusCode: 500);
            }
        });
    }
}