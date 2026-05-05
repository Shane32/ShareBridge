using ShareBridge.App.Models;
using System.Text.Json;
using Xunit;

namespace ShareBridge.Tests;

/// <summary>Tests for data models.</summary>
public sealed class ModelTests
{
    // -------------------------------------------------------------------------
    // ShareBridgeSettings

    [Fact]
    public void ShareBridgeSettings_Defaults_AreCorrect()
    {
        var s = new ShareBridgeSettings();
        Assert.Equal("outlook-classic", s.Destination);
        Assert.Equal(72, s.TempFileRetentionHours);
        Assert.Equal("", s.DefaultSubject);
        Assert.Equal("", s.DefaultBody);
        Assert.True(s.AttachFiles);
        Assert.False(s.ShowSuccessWindow);
        Assert.True(s.ShowErrors);
    }

    [Fact]
    public void ShareBridgeSettings_RoundTripsJson()
    {
        var original = new ShareBridgeSettings
        {
            Destination = "outlook-classic",
            TempFileRetentionHours = 48,
            DefaultSubject = "Shared image",
            DefaultBody = "See attached.",
            AttachFiles = true,
            ShowSuccessWindow = true,
            ShowErrors = false
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<ShareBridgeSettings>(json)!;

        Assert.Equal(original.Destination, restored.Destination);
        Assert.Equal(original.TempFileRetentionHours, restored.TempFileRetentionHours);
        Assert.Equal(original.DefaultSubject, restored.DefaultSubject);
        Assert.Equal(original.DefaultBody, restored.DefaultBody);
        Assert.Equal(original.AttachFiles, restored.AttachFiles);
        Assert.Equal(original.ShowSuccessWindow, restored.ShowSuccessWindow);
        Assert.Equal(original.ShowErrors, restored.ShowErrors);
    }

    // -------------------------------------------------------------------------
    // SharedPayload

    [Fact]
    public void SharedPayload_Properties_AreInitialized()
    {
        var receivedAt = DateTimeOffset.UtcNow;
        var files = new List<SharedFile>
        {
            new SharedFile { FileName = "snip.png", FullPath = "/tmp/snip.png", ContentType = "image/png" }
        };

        var payload = new SharedPayload
        {
            OperationId = "abc123",
            ReceivedAt = receivedAt,
            Files = files.AsReadOnly(),
            SourceFormats = new[] { "Bitmap" }
        };

        Assert.Equal("abc123", payload.OperationId);
        Assert.Equal(receivedAt, payload.ReceivedAt);
        Assert.Single(payload.Files);
        Assert.Single(payload.SourceFormats);
        Assert.Equal("Bitmap", payload.SourceFormats[0]);
    }

    // -------------------------------------------------------------------------
    // SharedFile

    [Fact]
    public void SharedFile_Properties_AreInitialized()
    {
        var file = new SharedFile
        {
            FileName = "snip.png",
            FullPath = @"C:\Users\test\AppData\Local\ShareBridge\Incoming\op\snip.png",
            ContentType = "image/png",
            Length = 12345
        };

        Assert.Equal("snip.png", file.FileName);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal(12345, file.Length);
    }
}
