namespace ShareBridge.App.Models;

/// <summary>
/// Application settings stored in %LOCALAPPDATA%\ShareBridge\settings.json.
/// </summary>
public sealed class ShareBridgeSettings
{
    /// <summary>Gets or sets the destination identifier. Currently only "outlook-classic" is supported.</summary>
    public string Destination { get; set; } = "outlook-classic";

    /// <summary>Gets or sets the number of hours to retain temporary share files before cleanup.</summary>
    public int TempFileRetentionHours { get; set; } = 72;

    /// <summary>Gets or sets the default subject line for new emails. Empty means no default.</summary>
    public string DefaultSubject { get; set; } = "";

    /// <summary>Gets or sets the default body text for new emails. Empty means no default.</summary>
    public string DefaultBody { get; set; } = "";

    /// <summary>Gets or sets whether to attach the shared files to the email.</summary>
    public bool AttachFiles { get; set; } = true;

    /// <summary>Gets or sets whether to show a success notification window after a successful share.</summary>
    public bool ShowSuccessWindow { get; set; } = false;

    /// <summary>Gets or sets whether to show an error window when a share operation fails.</summary>
    public bool ShowErrors { get; set; } = true;
}
