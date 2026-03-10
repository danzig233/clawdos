using Clawdos.Models;
using Clawdos.Services;

namespace Clawdos.Endpoints;

public static class WindowEndpoints
{
    public static void MapWindowEndpoints(this WebApplication app)
    {
        app.MapGet("/v1/window/list", (WindowManagementService wm) =>
        {
            return Results.Ok(wm.ListWindows());
        });
        app.MapPost("/v1/window/focus", (FocusRequest req, WindowManagementService wm) =>
        {
            if (req.TitleContains is null && req.ProcessName is null)
                return Results.BadRequest(new ApiError(
                    "At least one of titleContains or processName must be provided"));
            var found = wm.FocusWindow(req.TitleContains, req.ProcessName);
            if (!found)
                return Results.NotFound(new ApiError("No matching window found"));
            return Results.Ok(new ApiOk(true));
        });
    }
}
