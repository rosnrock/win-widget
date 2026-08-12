using System.Windows;
using WinWidget.Services;
using WinWidget.Views;

namespace WinWidget;

public partial class App : Application
{
    private SingleInstanceService? _singleInstance;
    private TrayIconService? _tray;
    private WidgetWindowManager? _widgetWindows;
    private ControlCenterWindow? _controlCenter;

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

        _widgetWindows = new WidgetWindowManager(new SettingsService());
        _controlCenter = new ControlCenterWindow(_widgetWindows);
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

    private void ExitApplication()
    {
        _controlCenter?.ClosePermanently();
        _widgetWindows?.CloseAll();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
