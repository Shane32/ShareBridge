using System.Text.Json;
using ShareBridge.App.Models;

namespace ShareBridge.App.Services;

/// <summary>
/// Manages temporary working folders under %LOCALAPPDATA%\ShareBridge\Incoming\.
/// Each share operation gets its own sub-folder.
/// </summary>
public sealed class TempFileService
{
    private static readonly JsonSerializerOptions _jsonOptions =
        new() { WriteIndented = true };

    private readonly string _incomingRoot;
    private readonly LoggingService _logger;

    public TempFileService(LoggingService logger)
    {
        _logger = logger;
        _incomingRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ShareBridge",
            "Incoming");
        Directory.CreateDirectory(_incomingRoot);
    }

    /// <summary>Creates a new operation folder with a timestamp-based name and returns its path.</summary>
    public string CreateOperationFolder(DateTimeOffset receivedAt)
    {
        // e.g. 2026-05-05T10-15-30-123
        var folderName = receivedAt.ToString("yyyy-MM-ddTHH-mm-ss-fff");
        var path = Path.Combine(_incomingRoot, folderName);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Copies a source file into the operation folder, preserving its extension
    /// and sanitizing the file name.
    /// </summary>
    public async Task<SharedFile> CopyFileAsync(
        string sourceFilePath,
        string operationFolder,
        string operationId,
        CancellationToken cancellationToken)
    {
        var originalName = Path.GetFileName(sourceFilePath);
        var safeName = SanitizeFileName(originalName);
        var destPath = GetUniqueDestPath(operationFolder, safeName);

        await CopyWithRetryAsync(sourceFilePath, destPath, cancellationToken);

        var info = new FileInfo(destPath);
        var contentType = GetContentTypeForExtension(Path.GetExtension(destPath));

        _logger.LogInfo(operationId, $"Copied file '{originalName}' -> '{destPath}'");

        return new SharedFile {
            FileName = Path.GetFileName(destPath),
            FullPath = destPath,
            ContentType = contentType,
            Length = info.Length
        };
    }

    /// <summary>
    /// Saves a stream to a new file in the operation folder with the given file name.
    /// </summary>
    public async Task<SharedFile> SaveStreamAsync(
        Stream stream,
        string fileName,
        string operationFolder,
        string operationId,
        CancellationToken cancellationToken)
    {
        var destPath = GetUniqueDestPath(operationFolder, fileName);

        await using var fs = new FileStream(
            destPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await stream.CopyToAsync(fs, cancellationToken);
        await fs.FlushAsync(cancellationToken);

        var info = new FileInfo(destPath);
        var contentType = GetContentTypeForExtension(Path.GetExtension(destPath));

        _logger.LogInfo(operationId, $"Saved stream -> '{destPath}' ({info.Length} bytes)");

        return new SharedFile {
            FileName = Path.GetFileName(destPath),
            FullPath = destPath,
            ContentType = contentType,
            Length = info.Length
        };
    }

    /// <summary>
    /// Writes a metadata.json file summarising the share operation into the operation folder.
    /// </summary>
    public async Task WriteMetadataAsync(
        string operationFolder,
        SharedPayload payload,
        CancellationToken cancellationToken)
    {
        var metadata = new {
            receivedAt = payload.ReceivedAt.ToString("O"),
            source = "Windows Share",
            formats = payload.SourceFormats,
            files = payload.Files.Select(f => new {
                fileName = f.FileName,
                contentType = f.ContentType
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(metadata, _jsonOptions);
        var metaPath = Path.Combine(operationFolder, "metadata.json");
        await File.WriteAllTextAsync(metaPath, json, cancellationToken);
    }

    /// <summary>Generates a safe, timestamp-based PNG file name for bitmap shares.</summary>
    public static string GenerateBitmapFileName(DateTimeOffset timestamp) =>
        $"Snip-{timestamp:yyyy-MM-dd-HHmmss}.png";

    /// <summary>Generates a safe, timestamp-based TXT file name for text shares.</summary>
    public static string GenerateTextFileName(DateTimeOffset timestamp) =>
        $"SharedText-{timestamp:yyyy-MM-dd-HHmmss}.txt";

    // -------------------------------------------------------------------------

    private static async Task CopyWithRetryAsync(
        string source,
        string dest,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++) {
            try {
                await using var src = new FileStream(
                    source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 81920, true);
                await using var dst = new FileStream(
                    dest, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
                await src.CopyToAsync(dst, cancellationToken);
                return;
            } catch (IOException) when (attempt < maxAttempts) {
                await Task.Delay(200 * attempt, cancellationToken);
            }
        }
    }

    private static string GetUniqueDestPath(string folder, string fileName)
    {
        var candidate = Path.Combine(folder, fileName);
        if (!File.Exists(candidate))
            return candidate;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        for (var i = 1; i < 1000; i++) {
            candidate = Path.Combine(folder, $"{nameWithoutExt}_{i}{ext}");
            if (!File.Exists(candidate))
                return candidate;
        }

        // Fallback with GUID
        return Path.Combine(folder, $"{nameWithoutExt}_{Guid.NewGuid():N}{ext}");
    }

    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return $"file_{Guid.NewGuid():N}";

        // Use the Windows-specific set of invalid characters so that the behaviour is
        // consistent regardless of which OS the code is compiled/tested on.
        // Windows forbids: " * : < > ? \ / | and control chars 0x00-0x1F
        ReadOnlySpan<char> windowsInvalid = stackalloc char[]
        {
            '"', '*', ':', '<', '>', '?', '\\', '/', '|',
            '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07',
            '\x08', '\x09', '\x0A', '\x0B', '\x0C', '\x0D', '\x0E', '\x0F',
            '\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', '\x17',
            '\x18', '\x19', '\x1A', '\x1B', '\x1C', '\x1D', '\x1E', '\x1F'
        };

        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++) {
            if (windowsInvalid.Contains(chars[i]))
                chars[i] = '_';
        }

        var safe = new string(chars).Trim(' ', '.');
        return string.IsNullOrEmpty(safe) ? $"file_{Guid.NewGuid():N}" : safe;
    }

    private static string? GetContentTypeForExtension(string ext) =>
        ext.ToLowerInvariant() switch {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".bmp" => "image/bmp",
            ".gif" => "image/gif",
            ".tif" or ".tiff" => "image/tiff",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => null
        };
}
