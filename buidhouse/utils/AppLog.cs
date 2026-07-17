namespace Simpleform.buidhouse.utils;

/// <summary>
/// Serilog error
/// → MissingMethodException khi chạy qua AddinManager.
/// </summary>
public static class AppLog
{
    private static readonly object Gate = new();
    private static bool _initialized;

    // Dev: log trong source để xem nhanh trên Cursor. Đổi path khi clone máy khác.
    private static readonly string LogDirectory = @"D:\revit\RevitAPI\buidhouse\utils\logs";
        // @"d:\source\Trungdev1611\revit_api\logs";

    public static string LogPath =>
        Path.Combine(
            LogDirectory,
            $"BuildHouse-{DateTime.Now:yyyy-MM-dd}.log");

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
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // không để logger làm crash command
        }
    }
}
