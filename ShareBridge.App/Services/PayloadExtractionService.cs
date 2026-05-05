using ShareBridge.App.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ShareBridge.App.Services;

/// <summary>
/// Inspects a <see cref="DataPackageView"/> and converts its content into a
/// <see cref="SharedPayload"/> containing local temporary files.
/// </summary>
public sealed class PayloadExtractionService
{
    private readonly TempFileService _tempFiles;
    private readonly LoggingService _logger;

    public PayloadExtractionService(TempFileService tempFiles, LoggingService logger)
    {
        _tempFiles = tempFiles;
        _logger = logger;
    }

    /// <summary>
    /// Extracts a normalised <see cref="SharedPayload"/> from the share operation.
    /// Priority: StorageItems → Bitmap → Text.
    /// </summary>
    public async Task<SharedPayload> ExtractAsync(
        ShareOperation shareOperation,
        string operationId,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        var data = shareOperation.Data;
        var formats = data.AvailableFormats.ToList();

        _logger.LogInfo(operationId, $"Available formats: {string.Join(", ", formats)}");

        var operationFolder = _tempFiles.CreateOperationFolder(receivedAt);
        var files = new List<SharedFile>();
        var usedFormats = new List<string>();

        // 1. StorageItems
        if (data.Contains(StandardDataFormats.StorageItems)) {
            _logger.LogInfo(operationId, "Extracting StorageItems");
            var items = await data.GetStorageItemsAsync().AsTask(cancellationToken);
            foreach (var item in items) {
                if (item is StorageFile storageFile) {
                    try {
                        var path = storageFile.Path;
                        if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
                            var shared = await _tempFiles.CopyFileAsync(
                                path, operationFolder, operationId, cancellationToken);
                            files.Add(shared);
                        } else {
                            // The file might be virtual (e.g. from Snipping Tool), stream it
                            using var streamWithContent = await storageFile.OpenReadAsync().AsTask(cancellationToken);
                            using var stream = streamWithContent.AsStreamForRead();
                            var safeName = TempFileService.SanitizeFileName(storageFile.Name);
                            var shared = await _tempFiles.SaveStreamAsync(
                                stream, safeName, operationFolder, operationId, cancellationToken);
                            files.Add(shared);
                        }
                    } catch (Exception ex) {
                        _logger.LogWarning(operationId, $"Failed to copy storage file '{storageFile.Name}'", ex);
                    }
                }
                // folders are intentionally ignored per spec
            }
            if (files.Count > 0)
                usedFormats.Add(StandardDataFormats.StorageItems);
        }

        // 2. Bitmap (if no files from StorageItems, or as supplement for Snipping Tool)
        if (files.Count == 0 && data.Contains(StandardDataFormats.Bitmap)) {
            _logger.LogInfo(operationId, "Extracting Bitmap");
            try {
                var streamRef = await data.GetBitmapAsync().AsTask(cancellationToken);
                using var stream = await streamRef.OpenReadAsync().AsTask(cancellationToken);
                using var readStream = stream.AsStreamForRead();
                var fileName = TempFileService.GenerateBitmapFileName(receivedAt);
                var shared = await _tempFiles.SaveStreamAsync(
                    readStream, fileName, operationFolder, operationId, cancellationToken);
                files.Add(shared);
                usedFormats.Add(StandardDataFormats.Bitmap);
            } catch (Exception ex) {
                _logger.LogWarning(operationId, "Failed to extract Bitmap", ex);
            }
        }

        // 3. Text (fallback)
        if (files.Count == 0 && data.Contains(StandardDataFormats.Text)) {
            _logger.LogInfo(operationId, "Extracting Text");
            try {
                var text = await data.GetTextAsync().AsTask(cancellationToken);
                if (!string.IsNullOrEmpty(text)) {
                    var fileName = TempFileService.GenerateTextFileName(receivedAt);
                    using var memStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
                    var shared = await _tempFiles.SaveStreamAsync(
                        memStream, fileName, operationFolder, operationId, cancellationToken);
                    files.Add(shared);
                    usedFormats.Add(StandardDataFormats.Text);
                }
            } catch (Exception ex) {
                _logger.LogWarning(operationId, "Failed to extract Text", ex);
            }
        }

        if (files.Count == 0) {
            _logger.LogWarning(operationId, "No supported content could be extracted from the share payload");
            throw new InvalidOperationException(
                "No supported share content was received. " +
                "Please share an image, file, or text.");
        }

        var payload = new SharedPayload {
            OperationId = operationId,
            ReceivedAt = receivedAt,
            Files = files.AsReadOnly(),
            SourceFormats = usedFormats.AsReadOnly()
        };

        // Write metadata alongside the files
        try {
            await _tempFiles.WriteMetadataAsync(operationFolder, payload, cancellationToken);
        } catch (Exception ex) {
            _logger.LogWarning(operationId, "Failed to write metadata.json", ex);
        }

        return payload;
    }
}
