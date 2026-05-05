using ShareBridge.App.Models;

namespace ShareBridge.App.Services;

/// <summary>
/// Creates a new Outlook Classic email with attachments via COM automation (late binding).
/// Does not send the email; only displays the compose window.
/// </summary>
public sealed class OutlookClassicService
{
    private const int OlMailItem = 0;

    private readonly LoggingService _logger;
    private readonly ShareBridgeSettings _settings;

    public OutlookClassicService(LoggingService logger, ShareBridgeSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    /// <summary>
    /// Returns true if Outlook Classic appears to be installed (ProgID is registered).
    /// </summary>
    public static bool IsOutlookInstalled() =>
        Type.GetTypeFromProgID("Outlook.Application") != null;

    /// <summary>
    /// Opens or connects to Outlook Classic, creates a new unsent email,
    /// attaches all files from the payload, and displays the compose window.
    /// Never sends the email.
    /// </summary>
    public Task CreateEmailWithAttachmentsAsync(
        SharedPayload payload,
        CancellationToken cancellationToken)
    {
        // COM automation must run on an STA thread
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                CreateEmailInternal(payload);
                tcs.SetResult(true);
            }
            catch (OperationCanceledException ex)
            {
                tcs.SetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        return tcs.Task;
    }

    private void CreateEmailInternal(SharedPayload payload)
    {
        var outlookType = Type.GetTypeFromProgID("Outlook.Application")
            ?? throw new InvalidOperationException(
                "Outlook Classic could not be started. " +
                "Please ensure Outlook Classic is installed.");

        _logger.LogInfo(payload.OperationId, "Creating Outlook COM instance");

        dynamic outlook = Activator.CreateInstance(outlookType)!;

        _logger.LogInfo(payload.OperationId, "Creating MailItem");

        dynamic mail = outlook.CreateItem(OlMailItem);

        if (!string.IsNullOrEmpty(_settings.DefaultSubject))
            mail.Subject = _settings.DefaultSubject;

        if (!string.IsNullOrEmpty(_settings.DefaultBody))
            mail.Body = _settings.DefaultBody;

        if (_settings.AttachFiles)
        {
            foreach (var file in payload.Files)
            {
                _logger.LogInfo(payload.OperationId, $"Attaching '{file.FullPath}'");
                mail.Attachments.Add(
                    file.FullPath,
                    /* olByValue */ 1,
                    /* Position */ Type.Missing,
                    /* DisplayName */ file.FileName);
            }
        }

        _logger.LogInfo(payload.OperationId, "Displaying compose window");

        // Display(false) = non-modal, does NOT send
        mail.Display(false);

        _logger.LogInfo(payload.OperationId, "Compose window displayed successfully");
    }
}
