using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using WinWidget.Models;

namespace WinWidget.Views;

public sealed class AppearanceWindow : Window
{
    private readonly AppearancePanel _panel = new();
    private WidgetWindow? _target;
    private bool _allowClose;

    public AppearanceWindow()
    {
        Title = "WinWidget — оформление";
        Width = 300;
        SizeToContent = SizeToContent.Height;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        Content = _panel;
        _panel.AppearanceChanged += OnAppearanceChanged;
        Deactivated += (_, _) => Hide();
        Closing += OnClosing;
    }

    public event EventHandler? AppearanceChanged;

    public void ShowFor(WidgetWindow target)
    {
        _target = target;
        var settings = target.Settings;
        _panel.Title = settings.Kind switch
        {
            WidgetKind.Clock => "Дата и время",
            WidgetKind.Calendar => "Календарь",
            WidgetKind.Notes => "Заметки",
            _ => "Оформление"
        };
        _panel.SetAppearance(ParseColor(settings.TextColor, Color.FromRgb(35, 71, 139)),
            ParseColor(settings.BackgroundColor, Colors.White), settings.BackgroundOpacity);

        Show();
        UpdateLayout();
        var virtualLeft = SystemParameters.VirtualScreenLeft + 12;
        var virtualTop = SystemParameters.VirtualScreenTop + 12;
        var virtualRight = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 12;
        var virtualBottom = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 12;
        var desiredLeft = target.Left + target.Width + 12;
        if (desiredLeft + ActualWidth > virtualRight)
            desiredLeft = target.Left - ActualWidth - 12;
        Left = Math.Clamp(desiredLeft, virtualLeft, Math.Max(virtualLeft, virtualRight - ActualWidth));
        Top = Math.Clamp(target.Top, virtualTop, Math.Max(virtualTop, virtualBottom - ActualHeight));
        Activate();
    }

    public void ClosePermanently()
    {
        _allowClose = true;
        Close();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;
        e.Cancel = true;
        Hide();
    }

    private void OnAppearanceChanged(object? sender, AppearanceChangedEventArgs e)
    {
        if (_target is null) return;
        _target.Settings.TextColor = ToHex(e.TextColor);
        _target.Settings.BackgroundColor = ToHex(e.BackgroundColor);
        _target.Settings.BackgroundOpacity = e.BackgroundOpacity;
        _target.ApplyAppearance();
        AppearanceChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static Color ParseColor(string value, Color fallback)
    {
        try { return (Color)ColorConverter.ConvertFromString(value); }
        catch { return fallback; }
    }
}
