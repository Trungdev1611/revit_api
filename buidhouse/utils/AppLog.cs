namespace Simpleform.buidhouse.utils;

/// <summary>
/// File logger đơn giản — tránh Serilog vì Revit/addin khác hay load Serilog.dll lệch version
/// → MissingMethodException khi chạy qua AddinManager.
/// </summary>
public static class AppLog
{
    private static readonly object Gate = new();
    private static bool _initialized;

    public static string LogPath { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "Simpleform-BuildHouse.log");

    public static void Init()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        Information("=== Simpleform logger ready → {0} ===", LogPath);
    }

    public static void Information(string messageTemplate, params object?[] args)
        => Write("INF", messageTemplate, null, args);

    public static void Warning(string messageTemplate, params object?[] args)
        => Write("WRN", messageTemplate, null, args);

    public static void Error(string messageTemplate, params object?[] args)
        => Write("ERR", messageTemplate, null, args);

    public static void Error(Exception ex, string messageTemplate, params object?[] args)
        => Write("ERR", messageTemplate, ex, args);

    public static void CloseAndFlush()
    {
        // no-op: mỗi Write đã flush
    }

    private static void Write(string level, string messageTemplate, Exception? ex, object?[] args)
    {
        try
        {
            string message = args is { Length: > 0 }
                ? string.Format(messageTemplate, args)
                : messageTemplate;

            string line = $"{DateTime.Now:HH:mm:ss.fff} [{level}] {message}";
            if (ex != null)
            {
                line += Environment.NewLine + ex;
            }

            lock (Gate)
            {
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // không để logger làm crash command
        }
    }
}
