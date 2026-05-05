using ShareBridge.App.Models;
using Windows.ApplicationModel.DataTransfer.ShareTarget;

namespace ShareBridge.App.Services;

/// <summary>
/// Orchestrates the full share-target activation pipeline:
/// receive → extract → dispatch → complete → cleanup.
/// </summary>
public sealed class ShareActivationService
{
    private readonly PayloadExtractionService _extractor;
    private readonly OutlookClassicService _outlook;
    private readonly CleanupService _cleanup;
    private readonly LoggingService _logger;
    private readonly ShareBridgeSettings _settings;

    public ShareActivationService(
        PayloadExtractionService extractor,
        OutlookClassicService outlook,
        CleanupService cleanup,
        LoggingService logger,
        ShareBridgeSettings settings)
    {
        _extractor = extractor;
        _outlook = outlook;
        _cleanup = cleanup;
        _logger = logger;
        _settings = settings;
    }

    /// <summary>
    /// Raised when the share operation fails with an error the user should see.
    /// </summary>
    public event EventHandler<ShareErrorEventArgs>? ErrorOccurred;

    /// <summary>
    /// Runs the complete share pipeline asynchronously.
    /// </summary>
    public async Task ProcessShareAsync(
        ShareOperation shareOperation,
        CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var receivedAt = DateTimeOffset.Now;

        _logger.LogInfo(operationId, "Share activation started");

        SharedPayload? payload = null;
        string? operationFolder = null;

        try {
            // Report to Windows that we have received the share
            shareOperation.ReportStarted();

            // Extract payload
            payload = await _extractor.ExtractAsync(
                shareOperation, operationId, receivedAt, cancellationToken);

            // Determine the operation folder from the first file
            if (payload.Files.Count > 0)
                operationFolder = Path.GetDirectoryName(payload.Files[0].FullPath);

            _logger.LogInfo(operationId,
                $"Extracted {payload.Files.Count} file(s): " +
                string.Join(", ", payload.Files.Select(f => f.FileName)));

            // Dispatch to Outlook
            await _outlook.CreateEmailWithAttachmentsAsync(payload, cancellationToken);

            _logger.LogInfo(operationId, "Share operation completed successfully");

            // Signal completion to Windows
            shareOperation.ReportCompleted();
        } catch (Exception ex) {
            _logger.LogError(operationId, "Share operation failed", ex);

            try { shareOperation.ReportError(ex.Message); } catch { /* best effort */ }

            if (_settings.ShowErrors)
                ErrorOccurred?.Invoke(this, new ShareErrorEventArgs(operationId, ex));
        } finally {
            // Opportunistic cleanup (never remove the active folder)
            try { _cleanup.RunCleanup(operationFolder); } catch (Exception ex) { _logger.LogWarning(operationId, "Cleanup failed", ex); }
        }
    }
}

/// <summary>Provides data for the <see cref="ShareActivationService.ErrorOccurred"/> event.</summary>
public sealed class ShareErrorEventArgs : EventArgs
{
    public ShareErrorEventArgs(string operationId, Exception exception)
    {
        OperationId = operationId;
        Exception = exception;
    }

    public string OperationId { get; }
    public Exception Exception { get; }
}
