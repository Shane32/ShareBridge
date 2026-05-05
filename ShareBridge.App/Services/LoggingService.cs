using System.Text;

namespace ShareBridge.App.Services;

/// <summary>
/// Writes structured log entries to daily log files under
/// %LOCALAPPDATA%\ShareBridge\Logs\.
/// </summary>
public sealed class LoggingService
{
    private readonly string _logFolder;
    private readonly object _lock = new();

    public LoggingService()
    {
        _logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ShareBridge",
            "Logs");
        Directory.CreateDirectory(_logFolder);
    }

    /// <summary>Gets the folder where log files are written.</summary>
    public string LogFolder => _logFolder;

    /// <summary>Gets the path of today's log file.</summary>
    public string TodayLogFile =>
        Path.Combine(_logFolder, $"sharebridge-{DateTimeOffset.Now:yyyy-MM-dd}.log");

    /// <summary>Writes an informational log entry.</summary>
    public void LogInfo(string operationId, string message) =>
        WriteEntry("INFO", operationId, message, null);

    /// <summary>Writes a warning log entry.</summary>
    public void LogWarning(string operationId, string message, Exception? ex = null) =>
        WriteEntry("WARN", operationId, message, ex);

    /// <summary>Writes an error log entry.</summary>
    public void LogError(string operationId, string message, Exception? ex = null) =>
        WriteEntry("ERROR", operationId, message, ex);

    private void WriteEntry(string level, string operationId, string message, Exception? ex)
    {
        var sb = new StringBuilder();
        sb.Append(DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"));
        sb.Append(" [");
        sb.Append(level.PadRight(5));
        sb.Append("] op=");
        sb.Append(operationId);
        sb.Append(' ');
        sb.Append(message);

        if (ex != null)
        {
            sb.AppendLine();
            sb.Append("  Exception: ");
            sb.Append(ex.GetType().FullName);
            sb.Append(": ");
            sb.Append(ex.Message);
            if (ex.StackTrace != null)
            {
                sb.AppendLine();
                foreach (var line in ex.StackTrace.Split('\n'))
                    sb.Append("    ").AppendLine(line.TrimEnd());
            }
            if (ex.InnerException != null)
            {
                sb.Append("  Inner: ");
                sb.Append(ex.InnerException.GetType().FullName);
                sb.Append(": ");
                sb.Append(ex.InnerException.Message);
            }
        }

        var logLine = sb.ToString();

        lock (_lock)
        {
            try
            {
                File.AppendAllText(TodayLogFile, logLine + Environment.NewLine);
            }
            catch
            {
                // Logging must never throw
            }
        }
    }
}
