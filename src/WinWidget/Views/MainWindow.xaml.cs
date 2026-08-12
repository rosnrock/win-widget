using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WinWidget.Models;
using WinWidget.Services;

namespace WinWidget.Views;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly ApplicationSettings _settings;
    private Border? _selectedSurface;
    private bool _isLoadingAppearance;
    private bool _allowClose;

    public MainWindow(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = settingsService.Load();
        InitializeComponent();
        if (NotesSurface.Child is NotesWidgetView notes)
        {
            notes.NoteChanged += (_, _) =>
            {
                if (_isLoadingAppearance) return;
                GetSettings(WidgetKind.Notes).Text = notes.NoteText;
                _settingsService.Save(_settings);
            };
        }
        Loaded += (_, _) =>
        {
            RestoreSettings();
            SelectSurface(ClockSurface);
        };
        Closing += OnClosing;
    }

    private void OnSelectWidget(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string name } && FindName(name) is Border surface)
            SelectSurface(surface);
    }

    private void OnWidgetMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border surface)
            SelectSurface(surface);
    }

    private void SelectSurface(Border surface)
    {
        if (_selectedSurface is not null)
        {
            _selectedSurface.BorderThickness = new Thickness(0);
            _selectedSurface.BorderBrush = null;
        }

        _selectedSurface = surface;
        surface.BorderBrush = new SolidColorBrush(Color.FromArgb(72, 35, 71, 139));
        surface.BorderThickness = new Thickness(1);
        LoadAppearance(surface);
    }

    private void OnAppearanceChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoadingAppearance || _selectedSurface is null || TextColor is null || BackgroundColor is null || SurfaceOpacity is null)
            return;

        TextElement.SetForeground(_selectedSurface, BrushFromSelection(TextColor, Color.FromRgb(35, 71, 139)));
        var color = ColorFromSelection(BackgroundColor, Colors.White);
        color.A = (byte)Math.Round(255 * SurfaceOpacity.Value / 100d);
        _selectedSurface.Background = new SolidColorBrush(color);
        if (OpacityLabel is not null)
            OpacityLabel.Text = $"{SurfaceOpacity.Value:0}%";
        StoreSurface(_selectedSurface, SettingsFor(_selectedSurface));
        _settingsService.Save(_settings);
    }

    public void ToggleLock()
    {
        var locked = !_settings.Widgets.All(widget => widget.IsLocked);
        foreach (var widget in _settings.Widgets) widget.IsLocked = locked;
        _settingsService.Save(_settings);
    }

    public void AllowClose()
    {
        SaveSettings();
        _allowClose = true;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        SaveSettings();
        if (_allowClose) return;
        e.Cancel = true;
        Hide();
    }

    private void RestoreSettings()
    {
        _isLoadingAppearance = true;
        RestoreSurface(ClockSurface, GetSettings(WidgetKind.Clock));
        RestoreSurface(CalendarSurface, GetSettings(WidgetKind.Calendar));
        RestoreSurface(NotesSurface, GetSettings(WidgetKind.Notes));
        if (NotesSurface.Child is NotesWidgetView notes)
            notes.NoteText = GetSettings(WidgetKind.Notes).Text;
        _isLoadingAppearance = false;
    }

    private void SaveSettings()
    {
        StoreSurface(ClockSurface, GetSettings(WidgetKind.Clock));
        StoreSurface(CalendarSurface, GetSettings(WidgetKind.Calendar));
        StoreSurface(NotesSurface, GetSettings(WidgetKind.Notes));
        if (NotesSurface.Child is NotesWidgetView notes)
            GetSettings(WidgetKind.Notes).Text = notes.NoteText;
        _settingsService.Save(_settings);
    }

    private WidgetSettings GetSettings(WidgetKind kind)
    {
        var settings = _settings.Widgets.FirstOrDefault(widget => widget.Kind == kind);
        if (settings is not null) return settings;
        settings = new WidgetSettings { Kind = kind };
        _settings.Widgets.Add(settings);
        return settings;
    }

    private WidgetSettings SettingsFor(Border surface) => GetSettings(
        surface == ClockSurface ? WidgetKind.Clock :
        surface == CalendarSurface ? WidgetKind.Calendar : WidgetKind.Notes);

    private static void RestoreSurface(Border surface, WidgetSettings settings)
    {
        var foreground = SafeColor(settings.TextColor, Color.FromRgb(35, 71, 139));
        TextElement.SetForeground(surface, new SolidColorBrush(foreground));
        var background = SafeColor(settings.BackgroundColor, Colors.White);
        background.A = (byte)Math.Round(255 * Math.Clamp(settings.BackgroundOpacity, 0, 1));
        surface.Background = new SolidColorBrush(background);
    }

    private static void StoreSurface(Border surface, WidgetSettings settings)
    {
        if (TextElement.GetForeground(surface) is SolidColorBrush foreground)
            settings.TextColor = $"#{foreground.Color.R:X2}{foreground.Color.G:X2}{foreground.Color.B:X2}";
        if (surface.Background is SolidColorBrush background)
        {
            settings.BackgroundColor = $"#{background.Color.R:X2}{background.Color.G:X2}{background.Color.B:X2}";
            settings.BackgroundOpacity = background.Color.A / 255d;
        }
    }

    private static Color SafeColor(string value, Color fallback)
    {
        try { return ColorFromString(value); }
        catch (FormatException) { return fallback; }
    }

    private void LoadAppearance(Border surface)
    {
        if (TextColor is null || BackgroundColor is null || SurfaceOpacity is null)
            return;

        _isLoadingAppearance = true;
        try
        {
            var foreground = TextElement.GetForeground(surface) as SolidColorBrush;
            var background = surface.Background as SolidColorBrush;
            SelectColor(TextColor, foreground?.Color ?? Color.FromRgb(35, 71, 139));
            SelectColor(BackgroundColor, background?.Color ?? Colors.White);
            SurfaceOpacity.Value = background?.Color.A / 255d * 100d ?? 0;
            OpacityLabel.Text = $"{SurfaceOpacity.Value:0}%";
        }
        finally
        {
            _isLoadingAppearance = false;
        }
    }

    private static void SelectColor(ComboBox box, Color color)
    {
        foreach (var item in box.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string value && ColorFromString(value) == Color.FromArgb(255, color.R, color.G, color.B))
            {
                box.SelectedItem = item;
                return;
            }
        }
    }

    private static Brush BrushFromSelection(ComboBox box, Color fallback) =>
        new SolidColorBrush(ColorFromSelection(box, fallback));

    private static Color ColorFromSelection(ComboBox box, Color fallback)
    {
        if (box.SelectedItem is ComboBoxItem { Tag: string value })
        {
            return ColorFromString(value);
        }
        return fallback;
    }

    private static Color ColorFromString(string value) =>
        (Color)ColorConverter.ConvertFromString(value);
}
