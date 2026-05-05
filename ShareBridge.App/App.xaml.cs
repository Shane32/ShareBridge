using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using ShareBridge.App.Models;
using ShareBridge.App.Services;
using Windows.ApplicationModel.Activation;

namespace ShareBridge.App;

/// <summary>
/// Provides application-scoped activation handling.
/// - Normal launch  → opens the status/settings <see cref="MainWindow"/>.
/// - Share activation → runs the share pipeline silently; shows an error
///   window only on failure.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    private readonly LoggingService _logger;
    private readonly ShareBridgeSettings _settings;

    public App()
    {
        this.InitializeComponent();
        _logger = new LoggingService();
        _settings = LoadSettings();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

        if (activationArgs.Kind == ExtendedActivationKind.ShareTarget) {
            HandleShareTargetActivation(activationArgs);
        } else {
            ShowMainWindow();
        }
    }

    // -------------------------------------------------------------------------
    // Share target activation

    private void HandleShareTargetActivation(AppActivationArguments activationArgs)
    {
        if (activationArgs.Data is not IShareTargetActivatedEventArgs shareArgs) {
            _logger.LogError("init", "Share activation args could not be cast to IShareTargetActivatedEventArgs");
            ShowMainWindow();
            return;
        }

        // WinUI 3 apps need at least one window to keep the message pump alive.
        // Use a minimal hidden background window during share processing.
        _window = new BackgroundWindow();
        _window.Activate();

        var shareOp = shareArgs.ShareOperation;
        var svc = BuildShareActivationService();

        svc.ErrorOccurred += (_, e) => {
            _window.DispatcherQueue.TryEnqueue(() => {
                var errorWindow = new ErrorWindow(e.Exception.Message, _logger.LogFolder);
                errorWindow.Activate();
            });
        };

        // Fire and forget on the thread-pool; the background window keeps the app alive
        _ = Task.Run(async () => {
            try {
                await svc.ProcessShareAsync(shareOp);
            } finally {
                // Close the background window to allow the app to exit
                _window.DispatcherQueue.TryEnqueue(() => _window.Close());
            }
        });
    }

    // -------------------------------------------------------------------------
    // Normal launch

    private void ShowMainWindow()
    {
        var cleanup = new CleanupService(_logger, _settings.TempFileRetentionHours);
        cleanup.RunCleanup();

        _window = new MainWindow(_logger, _settings);
        _window.Activate();
    }

    // -------------------------------------------------------------------------
    // Service construction

    private ShareActivationService BuildShareActivationService()
    {
        var tempFiles = new TempFileService(_logger);
        var extractor = new PayloadExtractionService(tempFiles, _logger);
        var outlook = new OutlookClassicService(_logger, _settings);
        var cleanup = new CleanupService(_logger, _settings.TempFileRetentionHours);
        return new ShareActivationService(extractor, outlook, cleanup, _logger, _settings);
    }

    // -------------------------------------------------------------------------
    // Settings

    private static ShareBridgeSettings LoadSettings()
    {
        var path = SettingsFilePath();
        if (File.Exists(path)) {
            try {
                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<ShareBridgeSettings>(json);
                if (loaded != null)
                    return loaded;
            } catch {
                // Fall through to defaults
            }
        }

        var defaults = new ShareBridgeSettings();
        SaveSettings(defaults, path);
        return defaults;
    }

    private static void SaveSettings(ShareBridgeSettings settings, string? path = null)
    {
        path ??= SettingsFilePath();
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        } catch {
            // Ignore save failures
        }
    }

    private static string SettingsFilePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ShareBridge",
            "settings.json");
}
