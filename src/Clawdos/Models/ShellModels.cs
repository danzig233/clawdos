namespace Clawdos.Models;

// ── Shell Request ───────────────────────────────────────
public sealed record ShellExecRequest(
    string   Command,
    string[]? Args          = null,
    string?  WorkingDir     = null,
    int      TimeoutMs      = 30_000,
    bool     MergeStdErr    = false);

// ── Shell Response ───────────────────────────────────────
public sealed record ShellExecResponse(
    bool    Ok,
    string  ActionId,
    int     ExitCode,
    string  Stdout,
    string  Stderr,
    long    ElapsedMs,
    bool    TimedOut);