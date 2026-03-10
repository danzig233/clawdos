using Clawdos.Configuration;

namespace Clawdos.Middleware;

/// <summary>
/// Checks the X-Api-Key request header.
/// /v1/health is always allowed (for external monitoring health checks).
/// </summary>
public sealed class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKey;

    public ApiKeyAuthMiddleware(RequestDelegate next, ClawdosConfig config)
    {
        _next   = next;
        _apiKey = config.ApiKey;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        // Health endpoint is always allowed, even without API key (for external monitoring)
        if (ctx.Request.Path.StartsWithSegments("/v1/health"))
        {
            await _next(ctx);
            return;
        }

        // API Key authentication for other endpoints
        if (!string.IsNullOrEmpty(_apiKey))
        {
            var provided = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
            if (provided != _apiKey)
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(new { error = "Unauthorized: invalid or missing X-Api-Key" });
                return;
            }
        }

        await _next(ctx);
    }
}