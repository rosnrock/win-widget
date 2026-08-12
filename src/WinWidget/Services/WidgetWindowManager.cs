using System.Windows;
using System.Windows.Controls;
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
    private readonly WeatherService _weatherService = new();
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };

    public IReadOnlyList<WidgetWindow> Windows => _windows;
    public IReadOnlyList<WidgetSettings> Widgets => _settings.Widgets;
    public ApplicationSettings Settings => _settings;
    public bool SnapToGrid
    {
        get => _settings.SnapToGrid;
        set => SetGrid(value, _settings.GridSize);
    }
    public double GridSize
    {
        get => _settings.GridSize;
        set => SetGrid(_settings.SnapToGrid, value);
    }
    public WidgetWindow? SelectedWindow { get; private set; }
    public event Action<WidgetWindow>? WidgetSelected;
    public event EventHandler? WidgetsChanged;

    public WidgetWindowManager(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = settingsService.Load();
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); SaveNow(); };
        _appearanceWindow.AppearanceChanged += (_, _) => Save();
        EnsureWidgetSettings();
        foreach (var settings in _settings.Widgets.ToArray()) CreateWindow(settings);
    }

    public WidgetSettings AddWidget(WidgetKind kind)
    {
        var count = _settings.Widgets.Count(widget => widget.Kind == kind) + 1;
        var settings = CreateDefaultSettings(kind, count);
        _settings.Widgets.Add(settings);
        var window = CreateWindow(settings);
        EnsureVisible(window);
        window.Show();
        SelectWindow(window, showAppearance: false);
        SaveNow();
        WidgetsChanged?.Invoke(this, EventArgs.Empty);
        return settings;
    }

    public bool RemoveWidget(string id)
    {
        var window = FindWindow(id);
        if (window is null) return false;
        // Keep the application useful: deleting the last widget is allowed, but the
        // control centre remains the way to add one again.
        if (ReferenceEquals(SelectedWindow, window)) SelectedWindow = null;
        _windows.Remove(window);
        _settings.Widgets.Remove(window.Settings);
        window.ClosePermanently();
        SaveNow();
        WidgetsChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool RemoveWidget(WidgetSettings settings) => RemoveWidget(settings.Id);

    public WidgetSettings? FindWidget(string id) => FindWindow(id)?.Settings;

    public void SetVisibility(string id, bool isVisible)
    {
        var window = FindWindow(id);
        if (window is null || window.Settings.IsVisible == isVisible) return;
        window.Settings.IsVisible = isVisible;
        if (isVisible) { EnsureVisible(window); window.Show(); }
        else window.Hide();
        SaveNow();
        WidgetsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetVisibility(WidgetSettings settings, bool isVisible) => SetVisibility(settings.Id, isVisible);

    public void FocusWidget(string id)
    {
        var window = FindWindow(id);
        if (window is null) return;
        if (!window.Settings.IsVisible) SetVisibility(id, true);
        EnsureVisible(window);
        window.Show();
        window.Activate();
        window.Topmost = true;
        window.Topmost = window.Settings.IsAlwaysOnTop;
        SelectWindow(window, showAppearance: false);
    }

    public void SelectWidget(string id)
    {
        var window = FindWindow(id);
        if (window is not null) SelectWindow(window, showAppearance: false);
    }

    public void ShowAll()
    {
        foreach (var window in _windows.Where(window => window.Settings.IsVisible))
        {
            EnsureVisible(window);
            window.Show();
        }
    }

    public void ShowSettings()
    {
        var target = SelectedWindow ?? _windows.FirstOrDefault(window => window.Settings.IsVisible);
        if (target is not null) _appearanceWindow.ShowFor(target);
    }

    public void ToggleLock()
    {
        var lockWidgets = !_settings.Widgets.All(widget => widget.IsLocked);
        foreach (var window in _windows) { window.Settings.IsLocked = lockWidgets; window.ApplyInteractionState(); }
        SaveNow();
        WidgetsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateAppearance(string id)
    {
        FindWindow(id)?.ApplyAppearance();
        SaveNow();
        WidgetsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateAppearance(WidgetKind kind)
    {
        foreach (var window in _windows.Where(window => window.Settings.Kind == kind)) window.ApplyAppearance();
        SaveNow();
        WidgetsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshWeather(string id)
    {
        var window = FindWindow(id);
        if (window?.Content is not Border { Child: WeatherWidgetView weatherView }) return;
        _ = weatherView.RefreshAsync();
        SaveNow();
        WidgetsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetGrid(bool enabled, double gridSize)
    {
        _settings.SnapToGrid = enabled;
        _settings.GridSize = double.IsFinite(gridSize) && gridSize >= 4 ? gridSize : 16;
        foreach (var window in _windows)
        {
            window.SnapToGrid = _settings.SnapToGrid;
            window.GridSize = _settings.GridSize;
        }
        SaveNow();
        WidgetsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AlignAllToGrid()
    {
        foreach (var window in _windows) window.AlignToGrid(force: true);
        SaveNow();
        WidgetsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CloseAll()
    {
        _saveTimer.Stop();
        SaveNow();
        _appearanceWindow.ClosePermanently();
        foreach (var window in _windows.ToArray()) window.ClosePermanently();
    }

    private WidgetWindow CreateWindow(WidgetSettings settings)
    {
        UserControl content = settings.Kind switch
        {
            WidgetKind.Clock => new ClockWidgetView(),
            WidgetKind.Calendar => new CalendarWidgetView(),
            WidgetKind.Notes => CreateNotesView(settings),
            WidgetKind.Weather => new WeatherWidgetView(settings, _weatherService, ScheduleSave),
            _ => throw new ArgumentOutOfRangeException(nameof(settings.Kind))
        };
        var window = new WidgetWindow(settings, content);
        window.SnapToGrid = _settings.SnapToGrid;
        window.GridSize = _settings.GridSize;
        window.Selected += (_, _) => SelectWindow(window, showAppearance: true);
        window.GeometryChanged += (_, _) => ScheduleSave();
        _windows.Add(window);
        return window;
    }

    private NotesWidgetView CreateNotesView(WidgetSettings settings)
    {
        var view = new NotesWidgetView { NoteText = settings.Text };
        view.NoteChanged += (_, _) => { settings.Text = view.NoteText; ScheduleSave(); };
        return view;
    }

    private void SelectWindow(WidgetWindow window, bool showAppearance)
    {
        SelectedWindow = window;
        WidgetSelected?.Invoke(window);
        if (showAppearance) _appearanceWindow.ShowFor(window);
    }

    private WidgetWindow? FindWindow(string id) =>
        _windows.FirstOrDefault(window => string.Equals(window.Settings.Id, id, StringComparison.OrdinalIgnoreCase));

    private void EnsureWidgetSettings()
    {
        if (_settings.Widgets.Count > 0) return;
        foreach (var kind in Enum.GetValues<WidgetKind>()) _settings.Widgets.Add(CreateDefaultSettings(kind, 1));
    }

    private WidgetSettings CreateDefaultSettings(WidgetKind kind, int number)
    {
        var settings = new WidgetSettings
        {
            Kind = kind,
            DisplayName = DefaultDisplayName(kind, number),
            Left = 70 + ((_settings.Widgets.Count * 36) % 360),
            Top = 80 + ((_settings.Widgets.Count * 36) % 240),
            // New widgets start as soft, dark macOS-style cards. These values are
            // assigned only at creation, so saved user colours remain untouched.
            BackgroundColor = "#10244A",
            TextColor = "#E8EDF9",
            BackgroundOpacity = 0.86
        };
        (settings.Width, settings.Height) = kind switch
        {
            WidgetKind.Clock => (440, 210),
            WidgetKind.Calendar => (230, 220),
            WidgetKind.Notes => (300, 170),
            WidgetKind.Weather => (320, 240),
            _ => (320, 180)
        };
        return settings;
    }

    private static string DefaultDisplayName(WidgetKind kind, int number)
    {
        var name = kind switch { WidgetKind.Clock => "Часы", WidgetKind.Calendar => "Календарь", WidgetKind.Notes => "Заметка", WidgetKind.Weather => "Погода", _ => "Виджет" };
        return number == 1 ? name : $"{name} {number}";
    }

    private void Save() => ScheduleSave();
    private void ScheduleSave() { _saveTimer.Stop(); _saveTimer.Start(); }
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
