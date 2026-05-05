# ShareBridge

ShareBridge is a Windows application that bridges the built-in Windows Share sheet to **Outlook Classic**. When you share a file, image, or text from any app using the Windows Share sheet, ShareBridge intercepts it and opens a new Outlook Classic compose window with the shared content attached — ready to send.

## Features

- **Windows Share Target** – appears as "Share to Outlook Classic" in the Windows Share sheet
- **Multiple content types** – supports files (images, PDFs, and more), bitmaps (screenshots from Snipping Tool), and plain text
- **COM automation** – integrates with Outlook Classic via COM late-binding; no extra interop packages needed
- **Configurable** – optional default subject, body, retention period for temp files, and error display settings
- **Packaged as MSIX** – installs as a modern Windows app; no administrator access required

## Requirements

- Windows 10 version 17763 or later (Windows 11 recommended)
- [Microsoft Outlook Classic](https://www.microsoft.com/en-us/microsoft-365) installed
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download) (included via self-contained build)

## Installation

### From a GitHub Release

1. Download the `.msix` and `ShareBridge.cer` files from the [latest release](../../releases/latest).
2. Install the signing certificate so Windows trusts the package:
   ```powershell
   Import-Certificate -FilePath ShareBridge.cer -CertStoreLocation Cert:\CurrentUser\TrustedPeople
   ```
3. Double-click the `.msix` file (or run `Add-AppxPackage ShareBridge_*.msix`) to install.
4. After installation, open the Windows Share sheet from any app and select **Share to Outlook Classic**.

### Build and Install Locally

```powershell
.\scripts\build-and-install.ps1
```

This script creates a local self-signed certificate, builds the MSIX package, trusts the certificate, and installs the app — all without administrator privileges.

## Configuration

Settings are stored in `%LOCALAPPDATA%\ShareBridge\settings.json` and are created on first launch with sensible defaults.

| Setting | Default | Description |
|---|---|---|
| `Destination` | `"outlook-classic"` | Share destination (only Outlook Classic is currently supported) |
| `TempFileRetentionHours` | `72` | How long (in hours) to keep temporary share files before cleanup |
| `DefaultSubject` | `""` | Pre-filled subject line for new emails |
| `DefaultBody` | `""` | Pre-filled body text for new emails |
| `AttachFiles` | `true` | Whether to attach shared files to the email |
| `ShowSuccessWindow` | `false` | Whether to show a notification after a successful share |
| `ShowErrors` | `true` | Whether to show an error window when a share operation fails |

## Project Structure

```
ShareBridge.sln
├── ShareBridge.App/          # WinUI 3 MSIX app (net10.0-windows10.0.22621.0)
│   ├── Services/             # Core logic: share activation, Outlook integration, temp files, logging
│   ├── Destinations/         # Share destination implementations
│   ├── Models/               # Data models (SharedPayload, SharedFile, settings)
│   ├── Interfaces/           # Service abstractions
│   └── Package.appxmanifest  # Windows app package manifest
└── ShareBridge.Tests/        # xUnit tests for platform-independent logic (net10.0)
```

## Contributing

Contributions are welcome! Please open an issue or pull request.

When working in this repository the CI will verify that:
- The project builds on Windows (`dotnet build`)
- All tests pass (`dotnet test`)
- Code is formatted correctly (`dotnet format --verify-no-changes`)

## License

[MIT](LICENSE)