using System.Windows;
using System.Windows.Threading;
using WinWidget.Models;
using WinWidget.Views;

namespace WinWidget.Services;

public sealed class WidgetWindowManager
{
    private readonly SettingsService _settingsService;
    private readonly ApplicationSettings _settings;
    private readonly List<WidgetWindow> _windows = [];
    private readonly AppearanceWindow _appearanceWindow = new();
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };

    public IReadOnlyList<WidgetWindow> Windows => _windows;
    public WidgetWindow? SelectedWindow { get; private set; }
    public event Action<WidgetWindow>? WidgetSelected;

    public WidgetWindowManager(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = settingsService.Load();
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            SaveNow();
        };
        _appearanceWindow.AppearanceChanged += (_, _) => Save();
        EnsureWidgetSettings();
        AddWindow(WidgetKind.Clock, new ClockWidgetView());
        AddWindow(WidgetKind.Calendar, new CalendarWidgetView());

        var notesView = new NotesWidgetView();
        var notesSettings = GetSettings(WidgetKind.Notes);
        notesView.NoteText = notesSettings.Text;
        notesView.NoteChanged += (_, _) =>
        {
            notesSettings.Text = notesView.NoteText;
            ScheduleSave();
        };
        AddWindow(WidgetKind.Notes, notesView);
    }

    public void ShowAll()
    {
        foreach (var window in _windows)
        {
            EnsureVisible(window);
            window.Show();
        }
    }

    public void ShowSettings()
    {
        var target = SelectedWindow ?? _windows.FirstOrDefault();
        if (target is not null) _appearanceWindow.ShowFor(target);
    }

    public void ToggleLock()
    {
        var lockWidgets = !_settings.Widgets.All(widget => widget.IsLocked);
        foreach (var window in _windows)
        {
            window.Settings.IsLocked = lockWidgets;
            window.ApplyInteractionState();
        }
        SaveNow();
    }

    public void UpdateAppearance(WidgetKind kind)
    {
        _windows.FirstOrDefault(window => window.Settings.Kind == kind)?.ApplyAppearance();
        SaveNow();
    }

    public void CloseAll()
    {
        _saveTimer.Stop();
        SaveNow();
        _appearanceWindow.ClosePermanently();
        foreach (var window in _windows) window.ClosePermanently();
    }

    private void AddWindow(WidgetKind kind, System.Windows.Controls.UserControl content)
    {
        var window = new WidgetWindow(GetSettings(kind), content);
        window.Selected += (_, _) =>
        {
            SelectedWindow = window;
            WidgetSelected?.Invoke(window);
            _appearanceWindow.ShowFor(window);
        };
        window.GeometryChanged += (_, _) => ScheduleSave();
        _windows.Add(window);
    }

    private void EnsureWidgetSettings()
    {
        foreach (var kind in Enum.GetValues<WidgetKind>()) GetSettings(kind);
    }

    private WidgetSettings GetSettings(WidgetKind kind)
    {
        var result = _settings.Widgets.FirstOrDefault(widget => widget.Kind == kind);
        if (result is not null) return result;
        result = new WidgetSettings { Kind = kind };
        _settings.Widgets.Add(result);
        return result;
    }

    private void Save() => ScheduleSave();

    private void ScheduleSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveNow() => _settingsService.Save(_settings);

    private static void EnsureVisible(Window window)
    {
        var left = SystemParameters.VirtualScreenLeft;
        var top = SystemParameters.VirtualScreenTop;
        var right = left + SystemParameters.VirtualScreenWidth;
        var bottom = top + SystemParameters.VirtualScreenHeight;
        const double visibleEdge = 40;
        if (window.Left + visibleEdge > right || window.Top + visibleEdge > bottom ||
            window.Left + window.Width - visibleEdge < left || window.Top + window.Height - visibleEdge < top)
        {
            window.Left = Math.Max(left, SystemParameters.WorkArea.Left + 40);
            window.Top = Math.Max(top, SystemParameters.WorkArea.Top + 40);
        }
    }
}
