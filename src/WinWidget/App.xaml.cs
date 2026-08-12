using System.IO;
using System.Windows;
using System.Windows.Threading;
using WinWidget.Services;
using WinWidget.Views;

namespace WinWidget;

public partial class App : Application
{
    private SingleInstanceService? _singleInstance;
    private TrayIconService? _tray;
    private WidgetWindowManager? _widgetWindows;
    private ControlCenterWindow? _controlCenter;
    private bool _isHandlingFatalError;

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            _singleInstance = new SingleInstanceService();
            if (!_singleInstance.IsPrimaryInstance)
            {
                Shutdown();
                return;
            }

            _widgetWindows = new WidgetWindowManager(new SettingsService());
            _controlCenter = new ControlCenterWindow(_widgetWindows);
            _singleInstance.ActivationRequested += OnActivationRequested;
            _singleInstance.StartListening();
            _tray = new TrayIconService();
            _tray.OpenRequested += (_, _) =>
            {
                _widgetWindows.ShowAll();
                _controlCenter.ShowAndActivate();
            };
            _tray.ToggleLockRequested += (_, _) => _widgetWindows.ToggleLock();
            _tray.ExitRequested += (_, _) => ExitApplication();
            _widgetWindows.ShowAll();
            _controlCenter.ShowAndActivate();
        }
        catch (Exception exception)
        {
            HandleFatalException(exception);
        }
    }

    private void OnActivationRequested(object? sender, EventArgs e) => Dispatcher.BeginInvoke(() =>
    {
        _controlCenter?.ShowAndActivate();
    });

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        HandleFatalException(e.Exception);
    }

    private void HandleFatalException(Exception exception)
    {
        if (_isHandlingFatalError) return;
        _isHandlingFatalError = true;
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinWidget", "crash.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTimeOffset.Now:O}]{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // Reporting must continue even when the profile cannot be written.
        }
        catch (UnauthorizedAccessException)
        {
            // The original exception remains the one reported to the user.
        }

        try
        {
            MessageBox.Show($"WinWidget не удалось запустить. Подробности записаны в:{Environment.NewLine}{logPath}",
                "WinWidget", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            try { _controlCenter?.ClosePermanently(); } catch { }
            try { _widgetWindows?.CloseAll(); } catch { }
            Shutdown(-1);
        }
    }

    private void ExitApplication()
    {
        _controlCenter?.ClosePermanently();
        _widgetWindows?.CloseAll();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        if (_singleInstance is not null) _singleInstance.ActivationRequested -= OnActivationRequested;
        _tray?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
