namespace Clawdos.Models;

// ── 通用响应 ──────────────────────────────────────────────
public sealed record ApiOk(bool Ok = true, string? ActionId = null);

public sealed record ApiError(string Error, bool Ok = false);

/// <summary>附带执行后截图的成功响应</summary>
public sealed record ApiOkWithCapture(
    bool Ok,
    string ActionId,
    AfterCaptureResult? AfterCapture = null);

public sealed record AfterCaptureResult(string Format, string Data);

// ── Health ────────────────────────────────────────────────
public sealed record HealthResponse(
    bool    Ok,
    string  Version,
    long    UptimeMs,
    long    TotalRequests,
    double  CaptureAvgMs,
    double  InputAvgMs,
    long    ErrorCount,
    string? LastRequestTime,
    string? LastErrorMessage);

// ── Env ──────────────────────────────────────────────────
public sealed record EnvResponse(
    int     ScreenWidth,
    int     ScreenHeight,
    double  DpiScale,
    string? TaskbarPosition,
    string? ActiveWindowTitle,
    string? ActiveProcessName,
    bool    ImeEnabled);
