namespace Clawdos.Models;

// ── Single Input Requests ─────────────────────────────────────────
public sealed record ClickRequest(
    int X, int Y,
    string Button = "left",
    int Count = 1,
    int? CaptureAfterMs = null);

public sealed record MoveRequest(int X, int Y);

public sealed record DragRequest(
    int FromX, int FromY,
    int ToX,   int ToY,
    string Button     = "left",
    int DurationMs    = 300,
    int? CaptureAfterMs = null);

public sealed record KeysRequest(
    string[] Combo,
    int? CaptureAfterMs = null);

public sealed record TypeRequest(
    string Text,
    bool UseClipboard     = false,
    int? CaptureAfterMs   = null);

public sealed record ScrollRequest(
    int Amount,
    int? X = null,
    int? Y = null,
    int? CaptureAfterMs = null);

// ── Batch Actions ─────────────────────────────────────────────
public sealed record BatchRequest(
    BatchAction[] Actions,
    int? CaptureAfterMs = null);

/// <summary>
/// Defines a single action in a batch request. The Type field determines which other fields are used.
/// </summary>
public sealed record BatchAction(
    string  Type,                     // click | move | drag | scroll | keys | type | wait
    int?    X            = null,
    int?    Y            = null,
    string? Button       = null,
    int?    Count        = null,
    int?    FromX        = null,
    int?    FromY        = null,
    int?    ToX          = null,
    int?    ToY          = null,
    int?    Amount       = null,
    int?    DurationMs   = null,
    string[]? Combo      = null,
    string? Text         = null,
    bool?   UseClipboard = null,
    int?    Ms           = null);

public sealed record BatchResponse(
    bool    Ok,
    string  ActionId,
    int     ExecutedCount,
    int?    FailedAtIndex           = null,
    string? Error                   = null,
    AfterCaptureResult? AfterCapture = null);
