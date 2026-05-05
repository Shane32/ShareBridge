using ShareBridge.App.Services;
using Xunit;

namespace ShareBridge.Tests;

/// <summary>Tests for <see cref="CleanupService"/>.</summary>
public sealed class CleanupServiceTests : IDisposable
{
    private readonly string _testIncomingRoot;
    private readonly LoggingService _logger;

    public CleanupServiceTests()
    {
        _testIncomingRoot = Path.Combine(Path.GetTempPath(), "SBCleanupTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testIncomingRoot);
        _logger = new LoggingService();
    }

    public void Dispose()
    {
        try { Directory.Delete(_testIncomingRoot, recursive: true); } catch { }
    }

    [Fact]
    public void RunCleanup_DeletesOldFolders()
    {
        // retentionHours=1; folder created 2 hours ago → should be deleted
        var svc = new CleanupService(_logger, retentionHours: 1, incomingRoot: _testIncomingRoot);

        var oldFolder = Path.Combine(_testIncomingRoot, "2026-01-01T00-00-00-000");
        Directory.CreateDirectory(oldFolder);
        Directory.SetCreationTimeUtc(oldFolder, DateTime.UtcNow.AddHours(-2));

        svc.RunCleanup();

        Assert.False(Directory.Exists(oldFolder));
    }

    [Fact]
    public void RunCleanup_KeepsRecentFolders()
    {
        var svc = new CleanupService(_logger, retentionHours: 72, incomingRoot: _testIncomingRoot);

        var recentFolder = Path.Combine(_testIncomingRoot, "2026-05-05T10-15-30-123");
        Directory.CreateDirectory(recentFolder);

        svc.RunCleanup();

        Assert.True(Directory.Exists(recentFolder));
    }

    [Fact]
    public void RunCleanup_SkipsActiveOperationFolder()
    {
        // retentionHours=1; folder is old but it's the active one → must not be deleted
        var svc = new CleanupService(_logger, retentionHours: 1, incomingRoot: _testIncomingRoot);

        var activeFolder = Path.Combine(_testIncomingRoot, "active");
        Directory.CreateDirectory(activeFolder);
        Directory.SetCreationTimeUtc(activeFolder, DateTime.UtcNow.AddHours(-2));

        svc.RunCleanup(activeOperationFolder: activeFolder);

        Assert.True(Directory.Exists(activeFolder));
    }

    [Fact]
    public void RunCleanup_NonExistentRoot_DoesNotThrow()
    {
        var missingRoot = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid().ToString("N"));
        var svc = new CleanupService(_logger, retentionHours: 72, incomingRoot: missingRoot);

        // Should not throw
        svc.RunCleanup();
    }
}
