using ShareBridge.App.Models;

namespace ShareBridge.App.Interfaces;

/// <summary>
/// Abstraction for share destinations. The first implementation is
/// <see cref="Destinations.OutlookClassicDestination"/>. Additional
/// destinations (e.g. copy-to-folder, Thunderbird, custom script) can be
/// added in the future without changing the activation pipeline.
/// </summary>
public interface IShareDestination
{
    /// <summary>Gets the stable identifier for this destination (e.g. "outlook-classic").</summary>
    string Id { get; }

    /// <summary>Gets the human-readable display name shown in the UI.</summary>
    string DisplayName { get; }

    /// <summary>Returns true when this destination is capable of handling the given payload.</summary>
    bool CanHandle(SharedPayload payload);

    /// <summary>
    /// Dispatches the payload to the destination.
    /// The implementation is responsible for opening the target application
    /// and presenting the content to the user. It must never send anything
    /// automatically.
    /// </summary>
    Task DispatchAsync(SharedPayload payload, CancellationToken cancellationToken);
}
