using ShareBridge.App.Services;
using Xunit;

namespace ShareBridge.Tests;

/// <summary>Tests for <see cref="LoggingService"/>.</summary>
public sealed class LoggingServiceTests : IDisposable
{
    private readonly string _tempLogFolder;

    public LoggingServiceTests()
    {
        _tempLogFolder = Path.Combine(Path.GetTempPath(), "SBLogTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempLogFolder);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempLogFolder, recursive: true); } catch { }
    }

    [Fact]
    public void LogFolder_Exists_AfterConstruction()
    {
        var svc = new LoggingService();
        Assert.True(Directory.Exists(svc.LogFolder));
    }

    [Fact]
    public void TodayLogFile_HasCorrectDateSuffix()
    {
        var svc = new LoggingService();
        var expected = $"sharebridge-{DateTimeOffset.Now:yyyy-MM-dd}.log";
        Assert.EndsWith(expected, svc.TodayLogFile, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LogInfo_WritesEntryToFile()
    {
        var svc = new LoggingService();
        var uniqueMessage = $"Hello from test {Guid.NewGuid():N}";
        svc.LogInfo("testop", uniqueMessage);

        var content = File.ReadAllText(svc.TodayLogFile);
        Assert.Contains(uniqueMessage, content);
        Assert.Contains("testop", content);
        Assert.Contains("INFO", content);
    }

    [Fact]
    public void LogError_IncludesExceptionDetails()
    {
        var svc = new LoggingService();
        var ex = new InvalidOperationException("Test exception message");
        svc.LogError("testop", "An error occurred", ex);

        var content = File.ReadAllText(svc.TodayLogFile);
        Assert.Contains("Test exception message", content);
        Assert.Contains("ERROR", content);
    }

    [Fact]
    public void LogWarning_WithoutException_WritesEntry()
    {
        var svc = new LoggingService();
        svc.LogWarning("warnop", "Just a warning");

        var content = File.ReadAllText(svc.TodayLogFile);
        Assert.Contains("Just a warning", content);
        Assert.Contains("WARN", content);
    }
}
