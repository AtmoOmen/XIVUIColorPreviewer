using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using XIVUIColorPreviewer.Services;

namespace XIVUIColorPreviewer;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        Log.Initialize();
        var logger = Log.For<App>();
        logger.LogInformation("Application starting. BaseDirectory: {BaseDir}", AppContext.BaseDirectory);

        UnhandledException += (_, e) =>
        {
            logger.LogCritical(e.Exception, "Unhandled exception");
            e.Handled = true;
        };

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
