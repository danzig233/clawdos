using System.Diagnostics;
using Clawdos.Services;

namespace Clawdos.Middleware;

/// <summary>
/// Records metrics for each request, including latency and error count.
/// Metrics are categorized by endpoint type (capture, input, other) based on the request path.
/// </summary>
public sealed class MetricsMiddleware
{
    private readonly RequestDelegate _next;

    public MetricsMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, HealthMetricsService metrics)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            metrics.RecordError(ex.Message);
            throw;
        }
        finally
        {
            sw.Stop();
            var path = ctx.Request.Path.Value ?? "";
            var category = path switch
            {
                _ when path.StartsWith("/v1/screen")  => MetricCategory.Capture,
                _ when path.StartsWith("/v1/input")   => MetricCategory.Input,
                _ => MetricCategory.Other
            };
            metrics.RecordRequest(sw.ElapsedMilliseconds, category,
                ctx.Response.StatusCode >= 400);
        }
    }
}
