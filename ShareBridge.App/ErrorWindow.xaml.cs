using System.Diagnostics;
using Microsoft.UI.Xaml;

namespace ShareBridge.App;

/// <summary>
/// Displays an error message with the option to open the log folder.
/// Shown when a share operation fails.
/// </summary>
public sealed partial class ErrorWindow : Window
{
    private readonly string _logFolder;

    public ErrorWindow(string errorMessage, string logFolder)
    {
        _logFolder = logFolder;

        this.InitializeComponent();

        Title = "Share Bridge – Error";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(480, 280));

        ErrorMessageText.Text = errorMessage;
    }

    private void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try {
            Process.Start(new ProcessStartInfo {
                FileName = _logFolder,
                UseShellExecute = true
            });
        } catch {
            // Best effort
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Close();
}
