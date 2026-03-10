using System.Diagnostics;
using System.Text;
using Clawdos.Configuration;
using Clawdos.Models;

namespace Clawdos.Services;

/// <summary>
/// Shell command execution service.
/// Security constraints:
///   1. Command whitelist (optional, enabled via config)
///   2. Working directory must be within workingDirs sandbox (or left empty for default)
///   3. Forced timeout (default 30s, max 120s)
///   4. Output truncation (stdout/stderr each max 1 MB)
///   5. cmd.exe built-in commands are automatically wrapped as cmd /c calls
/// </summary>
public sealed class ShellService
{
    private readonly ClawdosConfig _config;
    private readonly string[] _roots;

    private const int MaxTimeoutMs     = 120_000;
    private const int MaxOutputBytes   = 1 * 1024 * 1024; // 1 MB

    // cmd.exe built-in commands (no standalone .exe; must be invoked via cmd /c)
    private static readonly HashSet<string> CmdBuiltins = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "echo", "dir", "type", "cd", "set", "copy", "del",
        "mkdir", "md", "rmdir", "rd", "ren", "rename",
        "move", "cls", "title", "ver", "vol", "path",
        "pushd", "popd", "mklink", "assoc", "ftype"
    };

    // Default whitelist; when config value is empty, no whitelist restriction is applied
    private static readonly HashSet<string> DefaultAllowedCommands = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "cmd", "cmd.exe",
        "powershell", "powershell.exe",
        "pwsh", "pwsh.exe",
        "ping", "ping.exe",
        "ipconfig", "ipconfig.exe",
        "tasklist", "tasklist.exe",
        "systeminfo", "systeminfo.exe",
        "where", "where.exe",
        "dotnet", "dotnet.exe",
        "git", "git.exe",
        "python", "python.exe",
        "node", "node.exe",
        "npm", "npm.cmd",
        // cmd.exe built-ins also added to whitelist; auto-wrapped as cmd /c at runtime
        "echo", "dir", "type", "cd", "set", "copy", "del",
        "mkdir", "md", "rmdir", "rd", "ren", "rename",
        "move", "cls", "title", "ver", "vol", "path",
        "pushd", "popd", "mklink", "assoc", "ftype"
    };

    public ShellService(ClawdosConfig config)
    {
        _config = config;
        _roots = config.WorkingDirs
            .Select(d =>
            {
                var full = Path.GetFullPath(d);
                return full.TrimEnd(Path.DirectorySeparatorChar)
                       + Path.DirectorySeparatorChar;
            })
            .ToArray();
    }

    // ── Execute command ─────────────────────────────────
    public async Task<ShellExecResponse> ExecuteAsync(ShellExecRequest req)
    {
        var actionId = Guid.NewGuid().ToString();

        // 1. Whitelist validation
        var cmdName = Path.GetFileName(req.Command);
        if (_config.ShellAllowList is { Length: > 0 } allowList)
        {
            var allowed = new HashSet<string>(allowList,
                StringComparer.OrdinalIgnoreCase);
            if (!allowed.Contains(cmdName) && !allowed.Contains(req.Command))
                throw new UnauthorizedAccessException(
                    $"Command '{req.Command}' is not in the allow-list.");
        }
        else
        {
            // Use default whitelist
            if (!DefaultAllowedCommands.Contains(cmdName)
                && !DefaultAllowedCommands.Contains(req.Command))
                throw new UnauthorizedAccessException(
                    $"Command '{req.Command}' is not in the default allow-list. " +
                    "Configure 'shellAllowList' in clawdos-config.json to customize.");
        }

        // 2. Working directory validation
        string? workDir = null;
        if (!string.IsNullOrWhiteSpace(req.WorkingDir))
        {
            workDir = Path.GetFullPath(req.WorkingDir);
            var inSandbox = _roots.Any(r =>
                workDir.StartsWith(r, StringComparison.OrdinalIgnoreCase)
                || workDir.TrimEnd(Path.DirectorySeparatorChar)
                    .Equals(r.TrimEnd(Path.DirectorySeparatorChar),
                        StringComparison.OrdinalIgnoreCase));
            if (!inSandbox)
                throw new UnauthorizedAccessException(
                    $"Working directory '{req.WorkingDir}' is outside sandbox roots.");
            if (!Directory.Exists(workDir))
                throw new DirectoryNotFoundException(
                    $"Working directory not found: {req.WorkingDir}");
        }

        // 3. Timeout upper limit
        var timeout = Math.Clamp(req.TimeoutMs, 1000, MaxTimeoutMs);

        // 4. Build process
        //    If the command is a cmd.exe built-in, auto-wrap as cmd /c <command> [args...]
        var isBuiltin = CmdBuiltins.Contains(cmdName);
        var psi = new ProcessStartInfo
        {
            FileName               = isBuiltin ? "cmd.exe" : req.Command,
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            RedirectStandardInput  = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding  = Encoding.UTF8,
        };

        if (isBuiltin)
        {
            // cmd /c <builtin> [args...]
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add(req.Command);
        }
        if (req.Args is { Length: > 0 })
        {
            foreach (var arg in req.Args)
                psi.ArgumentList.Add(arg);
        }

        if (workDir is not null)
            psi.WorkingDirectory = workDir;

        // 5. Execute
        var sw = Stopwatch.StartNew();
        bool timedOut = false;

        using var proc = new Process { StartInfo = psi };

        var stdoutSb = new StringBuilder();
        var stderrSb = new StringBuilder();

        proc.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null && stdoutSb.Length < MaxOutputBytes)
                stdoutSb.AppendLine(e.Data);
        };
        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null && stderrSb.Length < MaxOutputBytes)
            {
                if (req.MergeStdErr)
                    stdoutSb.AppendLine(e.Data);
                else
                    stderrSb.AppendLine(e.Data);
            }
        };

        try
        {
            proc.Start();
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ShellExecResponse(
                Ok: false, ActionId: actionId,
                ExitCode: -1,
                Stdout: "",
                Stderr: $"Failed to start process: {ex.Message}",
                ElapsedMs: sw.ElapsedMilliseconds,
                TimedOut: false);
        }

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        // Wait for completion or timeout
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await proc.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = true;
            try { proc.Kill(entireProcessTree: true); }
            catch { /* best effort */ }
        }

        sw.Stop();

        var exitCode = timedOut ? -1 : proc.ExitCode;
        var stdout = stdoutSb.ToString();
        var stderr = stderrSb.ToString();

        // Truncation marker
        if (stdout.Length >= MaxOutputBytes)
            stdout += "\n... [TRUNCATED]";
        if (stderr.Length >= MaxOutputBytes)
            stderr += "\n... [TRUNCATED]";

        return new ShellExecResponse(
            Ok:        !timedOut && exitCode == 0,
            ActionId:  actionId,
            ExitCode:  exitCode,
            Stdout:    stdout.TrimEnd(),
            Stderr:    stderr.TrimEnd(),
            ElapsedMs: sw.ElapsedMilliseconds,
            TimedOut:  timedOut);
    }
}