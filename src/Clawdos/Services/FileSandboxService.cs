using Clawdos.Configuration;
using Clawdos.Models;
namespace Clawdos.Services;
/// <summary>
/// A file sandbox service that restricts file operations to specified root directories.
/// Rejects any paths that attempt to escape the sandbox (e.g., ../).
/// </summary>
public sealed class FileSandboxService
{
    private readonly string[] _roots;
    public FileSandboxService(ClawdosConfig config)
    {
        // Normalize all root paths and ensure directories exist
        _roots = config.WorkingDirs
            .Select(d =>
            {
                var full = Path.GetFullPath(d);
                if (!Directory.Exists(full))
                    Directory.CreateDirectory(full);
                return full.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            })
            .ToArray();
    }
    // ── Path Resolution and Validation ──────────────────────────────────
    /// <summary>
    /// Resolves the rootId and relative path into an absolute path, ensuring it stays within the corresponding root directory.
    /// </summary>
    private string ResolvePath(int rootId, string relativePath)
    {
        if (rootId < 0 || rootId >= _roots.Length)
            throw new ArgumentOutOfRangeException(
                nameof(rootId), $"Invalid rootId {rootId}. Available: 0..{_roots.Length - 1}");
        var root = _roots[rootId];
        // Combine and normalize the path to prevent escaping the root (e.g., via ../)
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
        // Ensure the resolved path is still within the root directory
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException(
                $"Path escapes sandbox root. Resolved: {fullPath}");
        return fullPath;
    }
    // ── List ────────────────────────────────────────────
    public FsListResponse List(int rootId, string path)
    {
        var fullPath = ResolvePath(rootId, path);
        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        var dir = new DirectoryInfo(fullPath);
        var entries = dir.EnumerateFileSystemInfos()
            .Select(e => new FsEntry(
                Name:  e.Name,
                Type:  e is DirectoryInfo ? "dir" : "file",
                Size:  e is FileInfo fi ? fi.Length : 0,
                Mtime: e.LastWriteTimeUtc.ToString("O")))
            .ToArray();
        return new FsListResponse(entries);
    }
    // ── Read ────────────────────────────────────────────
    public FsReadResponse Read(int rootId, string path)
    {
        var fullPath = ResolvePath(rootId, path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {path}");
        var bytes = File.ReadAllBytes(fullPath);
        return new FsReadResponse("base64", Convert.ToBase64String(bytes));
    }
    // ── Write ───────────────────────────────────────────
    public void Write(int rootId, string path, string data, bool overwrite)
    {
        var fullPath = ResolvePath(rootId, path);
        if (!overwrite && File.Exists(fullPath))
            throw new IOException($"File already exists: {path}. Set overwrite=true to replace.");
        // Ensure parent directory exists
        var parentDir = Path.GetDirectoryName(fullPath);
        if (parentDir != null && !Directory.Exists(parentDir))
            Directory.CreateDirectory(parentDir);
        var bytes = Convert.FromBase64String(data);
        File.WriteAllBytes(fullPath, bytes);
    }
    // ── Mkdir ───────────────────────────────────────────
    public void Mkdir(int rootId, string path)
    {
        var fullPath = ResolvePath(rootId, path);
        Directory.CreateDirectory(fullPath);
    }
    // ── Delete ──────────────────────────────────────────
    public void Delete(int rootId, string path, bool recursive)
    {
        var fullPath = ResolvePath(rootId, path);
        if (Directory.Exists(fullPath))
        {
            if (!recursive)
                throw new IOException(
                    $"Path is a directory. Set recursive=true to delete: {path}");
            Directory.Delete(fullPath, true);
        }
        else if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        else
        {
            throw new FileNotFoundException($"Path not found: {path}");
        }
    }
    // ── Move ────────────────────────────────────────────
    public void MoveEntry(int rootId, string from, string to, bool overwrite)
    {
        var srcPath = ResolvePath(rootId, from);
        var dstPath = ResolvePath(rootId, to);
        if (File.Exists(srcPath))
        {
            if (!overwrite && File.Exists(dstPath))
                throw new IOException($"Destination already exists: {to}");
            var dstDir = Path.GetDirectoryName(dstPath);
            if (dstDir != null && !Directory.Exists(dstDir))
                Directory.CreateDirectory(dstDir);
            File.Move(srcPath, dstPath, overwrite);
        }
        else if (Directory.Exists(srcPath))
        {
            if (Directory.Exists(dstPath) && !overwrite)
                throw new IOException($"Destination directory already exists: {to}");
            Directory.Move(srcPath, dstPath);
        }
        else
        {
            throw new FileNotFoundException($"Source not found: {from}");
        }
    }
}