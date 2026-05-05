using Microsoft.UI.Xaml;

namespace ShareBridge.App;

/// <summary>
/// Minimal hidden window used to keep the WinUI 3 message pump alive
/// during share target processing. The window is never shown to the user.
/// </summary>
public sealed partial class BackgroundWindow : Window
{
    public BackgroundWindow()
    {
        this.InitializeComponent();

        // Minimise the window so it doesn't appear in the taskbar / foreground
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1, 1));
        this.AppWindow.Hide();
    }
}
