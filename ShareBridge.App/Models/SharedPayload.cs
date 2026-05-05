namespace ShareBridge.App.Models;

/// <summary>
/// Represents the payload received from a Windows Share operation,
/// normalized into local temporary files ready for Outlook dispatch.
/// </summary>
public sealed class SharedPayload
{
    /// <summary>Gets the unique identifier for this share operation.</summary>
    public required string OperationId { get; init; }

    /// <summary>Gets the timestamp when the share was received.</summary>
    public required DateTimeOffset ReceivedAt { get; init; }

    /// <summary>Gets the local files extracted from the share payload.</summary>
    public required IReadOnlyList<SharedFile> Files { get; init; }

    /// <summary>Gets the original data formats advertised by the share source.</summary>
    public IReadOnlyList<string> SourceFormats { get; init; } = [];
}
