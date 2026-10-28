using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

// Restricted stdio tool registration for local Goose experiments.
// This is an MCP tool boundary, NOT an operating-system security sandbox.
// The full profile registers an explicit legacy type set and excludes this class.
public sealed class GooseSafeTools
{
    private const int MaxReadChars = 100_000;
    private const int MaxWriteBytes = 64 * 1024;
    private static readonly HashSet<string> WritableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".bas", ".json", ".py", ".cs"
    };

    public static void ValidateConfiguration() => _ = GetRoot();

    private static string GetRoot()
    {
        var setting = Environment.GetEnvironmentVariable("GOOSE_SAFE_ROOT");
        if (string.IsNullOrWhiteSpace(setting) || !Path.IsPathFullyQualified(setting))
            throw new InvalidOperationException("GOOSE_SAFE_ROOT must be an existing absolute directory.");
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(setting));
        if (!Directory.Exists(full) || Path.GetPathRoot(full) == full)
            throw new InvalidOperationException("GOOSE_SAFE_ROOT must be an existing non-volume-root directory.");
        // Refuse junctions/symlinks even in the configured root's ancestors.
        // Also refuse reparse components below the root when resolving each tool call.
        AssertNoReparseComponents(full);
        return full;
    }

    private static void AssertNoReparseComponents(string path)
    {
        var full = Path.GetFullPath(path);
        var volume = Path.GetPathRoot(full)!;
        var current = volume;
        var relative = Path.GetRelativePath(volume, full);
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (string.IsNullOrWhiteSpace(segment) || segment == ".") continue;
            current = Path.Combine(current, segment);
            if (File.Exists(current) || Directory.Exists(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new UnauthorizedAccessException("Goose safe profile refuses links and reparse points.");
            }
        }
    }

    private static string Resolve(string path, bool allowRoot = false)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.");
        var root = GetRoot();
        var full = Path.GetFullPath(Path.IsPathFullyQualified(path) ? path : Path.Combine(root, path));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var inside = string.Equals(full, root, comparison) ||
            full.StartsWith(root + Path.DirectorySeparatorChar, comparison);
        if (!inside || (!allowRoot && string.Equals(full, root, comparison)))
            throw new UnauthorizedAccessException("Path is outside the Goose test workspace.");
        // On Windows this also rejects NTFS alternate streams and drive-relative syntax.
        if (Path.GetRelativePath(root, full).Contains(':'))
            throw new UnauthorizedAccessException("Drive-relative paths and alternate streams are not allowed.");
        AssertNoReparseComponents(full);
        return full;
    }

    [McpServerTool(Name = "bridge_ping")]
    [Description("Test the isolated Goose MCP connection; no file or shell operation.")]
    public static string Ping() => "GOOSE_MCP_SAFE_OK";

    [McpServerTool(Name = "fs.list")]
    [Description("List a directory inside GOOSE_SAFE_ROOT only; paths outside the test workspace are rejected.")]
    public static Task<string> List(
        [Description("Directory path under the test root, or '.' for the root.")] string path = ".")
        => ComputerTools.FsList(Resolve(path, allowRoot: true));

    [McpServerTool(Name = "fs.read")]
    [Description("Read only a file inside GOOSE_SAFE_ROOT. Cannot access symlinks, other directories or machines.")]
    public static Task<string> Read(
        [Description("File path relative to the test root or an absolute path inside it.")] string path,
        [Description("Maximum characters or bytes (up to 100000).")] int max_chars = 16000)
    {
        if (max_chars < 1 || max_chars > MaxReadChars)
            throw new ArgumentOutOfRangeException(nameof(max_chars));
        return ComputerTools.FsRead(Resolve(path), max_chars);
    }

    [McpServerTool(Name = "fs.write")]
    [Description("Create or replace a small source/test text file inside GOOSE_SAFE_ROOT only. No delete, shell, Git or remote access.")]
    public static async Task<string> Write(
        [Description("File path under GOOSE_SAFE_ROOT. Existing parent directory required.")] string path,
        [Description("Complete UTF-8 replacement text (maximum 64 KiB).")] string content,
        [Description("Explicitly permit replacing a file already present. Default false.")] bool overwrite = false)
    {
        var destination = Resolve(path);
        if (!WritableExtensions.Contains(Path.GetExtension(destination)))
            throw new UnauthorizedAccessException("This file extension is not writable in Goose safe mode.");
        var bytes = new UTF8Encoding(false, true).GetBytes(content);
        if (bytes.Length > MaxWriteBytes)
            throw new ArgumentOutOfRangeException(nameof(content), "Write limit: 64 KiB.");
        var parent = Path.GetDirectoryName(destination)!;
        if (!Directory.Exists(parent))
            throw new DirectoryNotFoundException("Create test subdirectories outside the agent session.");
        if (!overwrite && File.Exists(destination))
            throw new IOException("File already exists. Pass overwrite=true for an intentional replacement.");

        // A sibling temp file and same-directory rename avoid partially written fixtures.
        var temporary = Path.Combine(parent, ".goose-safe-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await stream.WriteAsync(bytes);
            // Recheck before writing: fail if a link appeared during async I/O.
            _ = Resolve(destination);
            File.Move(temporary, destination, overwrite);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        return JsonSerializer.Serialize(new { path = destination, bytes_written = bytes.Length, overwrite });
    }
}
