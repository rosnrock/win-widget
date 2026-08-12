using System.Windows;
using WinWidget.Services;
using WinWidget.Views;

namespace WinWidget;

public partial class App : Application
{
    private SingleInstanceService? _singleInstance;
    private TrayIconService? _tray;
    private MainWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _singleInstance = new SingleInstanceService();
        if (!_singleInstance.IsPrimaryInstance)
        {
            MessageBox.Show("WinWidget is already running.", "WinWidget");
            Shutdown();
            return;
        }

        _window = new MainWindow(new SettingsService());
        MainWindow = _window;
        _tray = new TrayIconService();
        _tray.OpenRequested += (_, _) => ShowMainWindow();
        _tray.ToggleLockRequested += (_, _) => _window.ToggleLock();
        _tray.ExitRequested += (_, _) => ExitApplication();
        _window.Show();
    }

    private void ShowMainWindow()
    {
        if (_window is null) return;
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private void ExitApplication()
    {
        _window?.AllowClose();
        _window?.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
