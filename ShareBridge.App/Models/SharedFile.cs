namespace ShareBridge.App.Models;

/// <summary>
/// Represents a single local file that has been extracted from a share payload.
/// </summary>
public sealed class SharedFile
{
    /// <summary>Gets the file name (without directory path).</summary>
    public required string FileName { get; init; }

    /// <summary>Gets the full absolute path to the file on disk.</summary>
    public required string FullPath { get; init; }

    /// <summary>Gets the MIME content type, if known.</summary>
    public string? ContentType { get; init; }

    /// <summary>Gets the file size in bytes, if known.</summary>
    public long? Length { get; init; }
}
