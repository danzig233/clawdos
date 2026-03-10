namespace Clawdos.Models;

public sealed record WindowListResponse(WindowInfo[] Windows);

public sealed record WindowInfo(
    string     Hwnd,
    string     Title,
    string     ProcessName,
    WindowRect Rect,
    bool       IsActive);

public sealed record WindowRect(int X, int Y, int W, int H);

public sealed record FocusRequest(
    string? TitleContains = null,
    string? ProcessName   = null);
