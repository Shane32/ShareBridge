using ShareBridge.App.Interfaces;
using ShareBridge.App.Models;
using ShareBridge.App.Services;

namespace ShareBridge.App.Destinations;

/// <summary>
/// Share destination that creates a new unsent Outlook Classic email
/// with the payload files attached and displays the compose window.
/// </summary>
public sealed class OutlookClassicDestination : IShareDestination
{
    private readonly OutlookClassicService _service;

    public OutlookClassicDestination(OutlookClassicService service)
    {
        _service = service;
    }

    /// <inheritdoc/>
    public string Id => "outlook-classic";

    /// <inheritdoc/>
    public string DisplayName => "Outlook Classic";

    /// <inheritdoc/>
    public bool CanHandle(SharedPayload payload) =>
        OutlookClassicService.IsOutlookInstalled() && payload.Files.Count > 0;

    /// <inheritdoc/>
    public Task DispatchAsync(SharedPayload payload, CancellationToken cancellationToken) =>
        _service.CreateEmailWithAttachmentsAsync(payload, cancellationToken);
}
