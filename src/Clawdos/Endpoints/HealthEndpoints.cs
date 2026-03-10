using Clawdos.Models;
using Clawdos.Services;

namespace Clawdos.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/v1/health", (HealthMetricsService metrics) =>
        {
            return Results.Ok(new HealthResponse(
                Ok:               true,
                Version:          HealthMetricsService.Version,
                UptimeMs:         metrics.UptimeMs,
                TotalRequests:    metrics.TotalRequests,
                CaptureAvgMs:     Math.Round(metrics.CaptureAvgMs, 2),
                InputAvgMs:       Math.Round(metrics.InputAvgMs, 2),
                ErrorCount:       metrics.ErrorCount,
                LastRequestTime:  metrics.LastRequestTime,
                LastErrorMessage: metrics.LastErrorMessage));
        });
        app.MapGet("/v1/env", (EnvironmentService env) =>
        {
            return Results.Ok(env.GetEnv());
        });
    }
}
