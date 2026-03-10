namespace Clawdos.Models;

public sealed record FsListResponse(FsEntry[] Entries);

public sealed record FsEntry(
    string  Name,
    string  Type,       // "file" | "dir"
    long    Size,
    string? Mtime);

public sealed record FsReadResponse(string Encoding, string Data);

public sealed record FsWriteRequest(
    int    RootId,
    string Path,
    string Encoding,     // "base64"
    string Data,
    bool   Overwrite = false);

public sealed record FsMkdirRequest(int RootId, string Path);

public sealed record FsDeleteRequest(
    int    RootId,
    string Path,
    bool   Recursive = false);

public sealed record FsMoveRequest(
    int    RootId,
    string From,
    string To,
    bool   Overwrite = false);
