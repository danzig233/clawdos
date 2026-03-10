using Clawdos.Services;

namespace Clawdos.Endpoints;

public static class ScreenEndpoints
{
    public static void MapScreenEndpoints(this WebApplication app)
    {
        app.MapGet("/v1/screen/capture", (
            ScreenCaptureService capture,
            string? format,
            int? quality) =>
        {
            var fmt = format ?? "png";
            var q   = quality ?? 80;
            var bytes = capture.Capture(fmt, q);
            if (bytes is null)
            {
                // if capture returns null, it means the requested format is unsupported or an error occurred during capture
                return Results.StatusCode(503);
            }
            var contentType = fmt.ToLower() switch
            {
                "jpg" or "jpeg" => "image/jpeg",
                _ => "image/png"
            };
            return Results.File(bytes, contentType);
        });
    }
}
