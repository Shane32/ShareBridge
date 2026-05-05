namespace ShareBridge.App.Services;

/// <summary>
/// Deletes share-operation folders older than the configured retention period.
/// Runs on normal app launch and opportunistically after share activation.
/// Never deletes a folder that was recently created.
/// </summary>
public class CleanupService
{
    private readonly string _incomingRoot;
    private readonly LoggingService _logger;
    private readonly int _retentionHours;

    public CleanupService(LoggingService logger, int retentionHours = 72)
        : this(logger, retentionHours,
               Path.Combine(
                   Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                   "ShareBridge",
                   "Incoming"))
    {
    }

    /// <summary>
    /// Internal constructor used by tests to inject a custom incoming folder path.
    /// </summary>
    internal CleanupService(LoggingService logger, int retentionHours, string incomingRoot)
    {
        _logger = logger;
        _retentionHours = retentionHours;
        _incomingRoot = incomingRoot;
    }

    /// <summary>
    /// Deletes any operation folder whose creation time is older than <see cref="_retentionHours"/> hours.
    /// Swallows individual folder errors so that one bad entry doesn't stop the rest.
    /// </summary>
    /// <param name="activeOperationFolder">
    /// Full path of the current operation's folder; it will never be deleted even if old.
    /// </param>
    public void RunCleanup(string? activeOperationFolder = null)
    {
        if (!Directory.Exists(_incomingRoot))
            return;

        var cutoff = DateTimeOffset.Now.AddHours(-_retentionHours);
        var deleted = 0;

        foreach (var dir in Directory.EnumerateDirectories(_incomingRoot))
        {
            try
            {
                // Never delete the active operation folder
                if (!string.IsNullOrEmpty(activeOperationFolder) &&
                    string.Equals(dir, activeOperationFolder, StringComparison.OrdinalIgnoreCase))
                    continue;

                var info = new DirectoryInfo(dir);
                if (info.CreationTime <= cutoff.LocalDateTime)
                {
                    Directory.Delete(dir, recursive: true);
                    deleted++;
                    _logger.LogInfo("cleanup", $"Deleted old operation folder: {dir}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("cleanup", $"Could not delete folder: {dir}", ex);
            }
        }

        if (deleted > 0)
            _logger.LogInfo("cleanup", $"Cleanup complete: {deleted} folder(s) removed");
    }
}
