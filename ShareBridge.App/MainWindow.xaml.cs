using System.Diagnostics;
using System.Reflection;
using Microsoft.UI.Xaml;
using ShareBridge.App.Models;
using ShareBridge.App.Services;

namespace ShareBridge.App;

/// <summary>
/// Status window shown when the app is launched normally (not via Share).
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly LoggingService _logger;
    private readonly ShareBridgeSettings _settings;

    public MainWindow(LoggingService logger, ShareBridgeSettings settings)
    {
        _logger = logger;
        _settings = settings;

        this.InitializeComponent();

        Title = "Share Bridge";
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(520, 400));

        PopulateStatus();
    }

    private void PopulateStatus()
    {
        OutlookStatusText.Text = OutlookClassicService.IsOutlookInstalled()
            ? "✔ Detected"
            : "✘ Not found – please install Outlook Classic";

        LogFolderLink.Content = _logger.LogFolder;

        RetentionText.Text = $"{_settings.TempFileRetentionHours} hours";

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : string.Empty;
    }

    private void LogFolderLink_Click(object sender, RoutedEventArgs e)
    {
        try {
            Process.Start(new ProcessStartInfo {
                FileName = _logger.LogFolder,
                UseShellExecute = true
            });
        } catch {
            // Best effort
        }
    }
}
