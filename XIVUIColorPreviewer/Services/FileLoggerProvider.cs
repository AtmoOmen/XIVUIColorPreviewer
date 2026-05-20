using Microsoft.Extensions.Logging;

namespace XIVUIColorPreviewer.Services;

/// <summary>
///     A minimal file logger provider that writes log entries to a daily-rotated file.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly LogLevel _minLevel;

    public FileLoggerProvider(string logDirectory, LogLevel minLevel = LogLevel.Debug)
    {
        _logDirectory = logDirectory;
        _minLevel = minLevel;
        Directory.CreateDirectory(_logDirectory);
    }

    public ILogger CreateLogger(string categoryName) =>
        new FileLogger(categoryName, _logDirectory, _minLevel);

    public void Dispose() { }

    private sealed class FileLogger(string categoryName, string logDirectory, LogLevel minLevel) : ILogger
    {
        private static readonly Lock s_lock = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= minLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            var logFile = Path.Combine(logDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
            var shortCategory = categoryName.Contains('.')
                ? categoryName[(categoryName.LastIndexOf('.') + 1)..]
                : categoryName;
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{logLevel,-11}] [{shortCategory}] {message}";

            if (exception != null)
                line += Environment.NewLine + exception;

            line += Environment.NewLine;

            lock (s_lock)
            {
                File.AppendAllText(logFile, line);
            }
        }
    }
}
