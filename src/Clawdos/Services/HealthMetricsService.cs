using System.Diagnostics;

namespace Clawdos.Services;

public enum MetricCategory { Capture, Input, Other }

/// <summary>
/// A thread-safe health metrics aggregator for use with /v1/health.
/// </summary>
public sealed class HealthMetricsService
{
    private readonly Stopwatch _uptime = Stopwatch.StartNew();

    private long   _totalRequests;
    private long   _errorCount;
    private long   _captureTotalMs;
    private long   _captureCount;
    private long   _inputTotalMs;
    private long   _inputCount;
    private string? _lastRequestTime;
    private string? _lastErrorMessage;

    public const string Version = "clawdos-0.1.0";

    // ── Write (called by MetricsMiddleware) ─────────────────────
    public void RecordRequest(long elapsedMs, MetricCategory cat, bool isError)
    {
        Interlocked.Increment(ref _totalRequests);
        Volatile.Write(ref _lastRequestTime, DateTime.UtcNow.ToString("O"));

        switch (cat)
        {
            case MetricCategory.Capture:
                Interlocked.Add(ref _captureTotalMs, elapsedMs);
                Interlocked.Increment(ref _captureCount);
                break;
            case MetricCategory.Input:
                Interlocked.Add(ref _inputTotalMs, elapsedMs);
                Interlocked.Increment(ref _inputCount);
                break;
        }

        if (isError)
            Interlocked.Increment(ref _errorCount);
    }

    public void RecordError(string message)
    {
        Interlocked.Increment(ref _errorCount);
        Volatile.Write(ref _lastErrorMessage, message);
    }

    // ── Read (called by HealthEndpoints) ──────────────────────
    public long    UptimeMs         => _uptime.ElapsedMilliseconds;
    public long    TotalRequests    => Interlocked.Read(ref _totalRequests);
    public long    ErrorCount       => Interlocked.Read(ref _errorCount);
    public string? LastRequestTime  => Volatile.Read(ref _lastRequestTime);
    public string? LastErrorMessage => Volatile.Read(ref _lastErrorMessage);

    public double CaptureAvgMs
    {
        get
        {
            var c = Interlocked.Read(ref _captureCount);
            return c == 0 ? 0 : (double)Interlocked.Read(ref _captureTotalMs) / c;
        }
    }

    public double InputAvgMs
    {
        get
        {
            var c = Interlocked.Read(ref _inputCount);
            return c == 0 ? 0 : (double)Interlocked.Read(ref _inputTotalMs) / c;
        }
    }
}
