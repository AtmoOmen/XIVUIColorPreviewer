using Microsoft.Extensions.Logging;

namespace XIVUIColorPreviewer.Services;

/// <summary>
///     Global logger factory for the application.
///     Call <see cref="Initialize"/> once at startup.
/// </summary>
public static class Log
{
    private static ILoggerFactory _factory = LoggerFactory.Create(_ => { });

    public static void Initialize()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "XIVUIColorPreviewer",
            "logs");

        _factory = LoggerFactory.Create(builder =>
        {
#if DEBUG
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddDebug();
#else
            builder.SetMinimumLevel(LogLevel.Information);
#endif
            builder.AddProvider(new FileLoggerProvider(logDir));
        });
    }

    public static ILogger<T> For<T>() => _factory.CreateLogger<T>();

    public static ILogger ForName(string name) => _factory.CreateLogger(name);
}
