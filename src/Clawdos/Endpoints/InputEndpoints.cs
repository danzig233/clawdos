using Clawdos.Models;
using Clawdos.Services;

namespace Clawdos.Endpoints;

public static class InputEndpoints
{
    public static void MapInputEndpoints(this WebApplication app)
    {
        // ── Click ─────────────────────────────────────
        app.MapPost("/v1/input/click", (ClickRequest req, InputInjectionService input) =>
        {
            try
            {
                input.Click(req.X, req.Y, req.Button, req.Count);
                var actionId = Guid.NewGuid().ToString();
                var afterCap = input.CaptureAfter(req.CaptureAfterMs);
                if (afterCap is not null)
                    return Results.Ok(new ApiOkWithCapture(true, actionId, afterCap));
                return Results.Ok(new ApiOk(true, actionId));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new ApiError(ex.Message));
            }
        });
        // ── Move ──────────────────────────────────────
        app.MapPost("/v1/input/move", (MoveRequest req, InputInjectionService input) =>
        {
            try
            {
                input.Move(req.X, req.Y);
                return Results.Ok(new ApiOk(true));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new ApiError(ex.Message));
            }
        });
        // ── Drag ──────────────────────────────────────
        app.MapPost("/v1/input/drag", (DragRequest req, InputInjectionService input) =>
        {
            try
            {
                input.Drag(req.FromX, req.FromY, req.ToX, req.ToY,
                    req.Button, req.DurationMs);
                var actionId = Guid.NewGuid().ToString();
                var afterCap = input.CaptureAfter(req.CaptureAfterMs);
                if (afterCap is not null)
                    return Results.Ok(new ApiOkWithCapture(true, actionId, afterCap));
                return Results.Ok(new ApiOk(true, actionId));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new ApiError(ex.Message));
            }
        });
        // ── Scroll ────────────────────────────────────
        app.MapPost("/v1/input/scroll", (ScrollRequest req, InputInjectionService input) =>
        {
            try
            {
                input.Scroll(req.Amount, req.X, req.Y);
                var actionId = Guid.NewGuid().ToString();
                var afterCap = input.CaptureAfter(req.CaptureAfterMs);
                if (afterCap is not null)
                    return Results.Ok(new ApiOkWithCapture(true, actionId, afterCap));
                return Results.Ok(new ApiOk(true, actionId));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new ApiError(ex.Message));
            }
        });
        // ── Keys ──────────────────────────────────────
        app.MapPost("/v1/input/keys", (KeysRequest req, InputInjectionService input) =>
        {
            try
            {
                input.Keys(req.Combo);
                var actionId = Guid.NewGuid().ToString();
                var afterCap = input.CaptureAfter(req.CaptureAfterMs);
                if (afterCap is not null)
                    return Results.Ok(new ApiOkWithCapture(true, actionId, afterCap));
                return Results.Ok(new ApiOk(true, actionId));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ApiError(ex.Message));
            }
        });
        // ── Type ──────────────────────────────────────
        app.MapPost("/v1/input/type", (TypeRequest req, InputInjectionService input) =>
        {
            input.TypeText(req.Text, req.UseClipboard);
            var actionId = Guid.NewGuid().ToString();
            var afterCap = input.CaptureAfter(req.CaptureAfterMs);
            if (afterCap is not null)
                return Results.Ok(new ApiOkWithCapture(true, actionId, afterCap));
            return Results.Ok(new ApiOk(true, actionId));
        });
        // ── Batch ─────────────────────────────────────
        app.MapPost("/v1/input/batch", (BatchRequest req, InputInjectionService input) =>
        {
            var actionId = Guid.NewGuid().ToString();
            var (executed, failedAt, error) = input.ExecuteBatch(req.Actions);
            var afterCap = input.CaptureAfter(req.CaptureAfterMs);
            if (failedAt is not null)
            {
                return Results.Ok(new BatchResponse(
                    Ok: false, ActionId: actionId,
                    ExecutedCount: executed, FailedAtIndex: failedAt,
                    Error: error, AfterCapture: afterCap));
            }
            return Results.Ok(new BatchResponse(
                Ok: true, ActionId: actionId,
                ExecutedCount: executed, AfterCapture: afterCap));
        });
    }
}
