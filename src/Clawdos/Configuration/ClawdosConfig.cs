namespace Clawdos.Configuration;

/// <summary>
/// Mapping clawdos-config.json POCO。
/// supports partial fields being overridden by environment variables (see Program.cs).
/// </summary>
public sealed class ClawdosConfig
{
    public string   ClientId   { get; set; } = "w10-default";
    public string   ListenIp   { get; set; } = "127.0.0.1";
    public int      Port       { get; set; } = 17171;
    public string   ApiKey     { get; set; } = string.Empty;
    public string[] WorkingDirs { get; set; } = Array.Empty<string>();
    public string[]? ShellAllowList { get; set; } = null;
}
