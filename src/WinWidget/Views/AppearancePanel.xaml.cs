using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WinWidget.Views;

public sealed class AppearanceChangedEventArgs(Color textColor, Color backgroundColor, double backgroundOpacity) : EventArgs
{
    public Color TextColor { get; } = textColor;
    public Color BackgroundColor { get; } = backgroundColor;
    public double BackgroundOpacity { get; } = backgroundOpacity;
}

public partial class AppearancePanel : UserControl
{
    private static readonly Brush NormalBorder = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
    private static readonly Brush InvalidBorder = new SolidColorBrush(Color.FromRgb(198, 40, 40));
    private bool _loading, _dragSv, _dragHue;
    private double _hue, _saturation, _value;

    public AppearancePanel() { InitializeComponent(); Loaded += (_, _) => LoadTargetColor(); }
    public event EventHandler<AppearanceChangedEventArgs>? AppearanceChanged;
    public Color SelectedTextColor { get; private set; } = Color.FromRgb(35, 71, 139);
    public Color SelectedBackgroundColor { get; private set; } = Colors.White;
    public double BackgroundOpacity => OpacitySlider.Value / 100d;
    public string Title { get => PanelTitle.Text; set => PanelTitle.Text = value; }

    public void SetAppearance(Color textColor, Color backgroundColor, double backgroundOpacity)
    {
        _loading = true;
        SelectedTextColor = Opaque(textColor); SelectedBackgroundColor = Opaque(backgroundColor);
        OpacitySlider.Value = Math.Clamp(backgroundOpacity, 0, 1) * 100;
        _loading = false; LoadTargetColor(); UpdateOpacity();
    }

    private Color ActiveColor { get => BackgroundTarget.IsChecked == true ? SelectedBackgroundColor : SelectedTextColor; set { if (BackgroundTarget.IsChecked == true) SelectedBackgroundColor = value; else SelectedTextColor = value; } }
    private void OnTargetChanged(object sender, RoutedEventArgs e) { if (IsLoaded && !_loading) LoadTargetColor(); }

    private void LoadTargetColor()
    {
        if (ColorR is null) return;
        RgbToHsv(ActiveColor, out _hue, out _saturation, out _value);
        SyncControls();
    }

    private void OnSvMouseDown(object sender, MouseButtonEventArgs e) { _dragSv = true; SvCanvas.CaptureMouse(); PickSv(e.GetPosition(SvCanvas)); }
    private void OnSvMouseMove(object sender, MouseEventArgs e) { if (_dragSv && e.LeftButton == MouseButtonState.Pressed) PickSv(e.GetPosition(SvCanvas)); }
    private void OnHueMouseDown(object sender, MouseButtonEventArgs e) { _dragHue = true; HueCanvas.CaptureMouse(); PickHue(e.GetPosition(HueCanvas).Y); }
    private void OnHueMouseMove(object sender, MouseEventArgs e) { if (_dragHue && e.LeftButton == MouseButtonState.Pressed) PickHue(e.GetPosition(HueCanvas).Y); }
    private void OnPickerMouseUp(object sender, MouseButtonEventArgs e) { _dragSv = _dragHue = false; Mouse.Capture(null); }
    private void OnPickerSizeChanged(object sender, SizeChangedEventArgs e) { if (IsLoaded) PositionMarkers(); }

    private void PickSv(Point p) { _saturation = Clamp01(p.X / Math.Max(1, SvCanvas.ActualWidth)); _value = 1 - Clamp01(p.Y / Math.Max(1, SvCanvas.ActualHeight)); ApplyHsv(); }
    private void PickHue(double y) { _hue = 360 * Clamp01(y / Math.Max(1, HueCanvas.ActualHeight)); ApplyHsv(); }
    private void ApplyHsv() { ActiveColor = HsvToRgb(_hue, _saturation, _value); SyncControls(); RaiseChanged(); }

    private void OnRgbChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || ColorR is null) return;
        var okR = byte.TryParse(ColorR.Text, out var r); var okG = byte.TryParse(ColorG.Text, out var g); var okB = byte.TryParse(ColorB.Text, out var b);
        ColorR.BorderBrush = okR ? NormalBorder : InvalidBorder; ColorG.BorderBrush = okG ? NormalBorder : InvalidBorder; ColorB.BorderBrush = okB ? NormalBorder : InvalidBorder;
        ColorError.Text = "Enter whole numbers from 0 to 255"; ColorError.Visibility = okR && okG && okB ? Visibility.Collapsed : Visibility.Visible;
        if (!(okR && okG && okB)) return;
        ActiveColor = Color.FromRgb(r, g, b); RgbToHsv(ActiveColor, out _hue, out _saturation, out _value); SyncControls(); RaiseChanged();
    }

    private void OnHexChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || ColorHex is null) return;
        var hex = ColorHex.Text.Trim().TrimStart('#');
        if (hex.Length != 6 || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb)) { ColorHex.BorderBrush = InvalidBorder; ColorError.Text = "Use HEX format #RRGGBB"; ColorError.Visibility = Visibility.Visible; return; }
        ActiveColor = Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb); RgbToHsv(ActiveColor, out _hue, out _saturation, out _value); SyncControls(); RaiseChanged();
    }

    private void SyncControls()
    {
        if (ColorR is null) return; _loading = true; var c = ActiveColor;
        ColorR.Text = c.R.ToString(CultureInfo.InvariantCulture); ColorG.Text = c.G.ToString(CultureInfo.InvariantCulture); ColorB.Text = c.B.ToString(CultureInfo.InvariantCulture); ColorHex.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        foreach (var b in new[] { ColorR, ColorG, ColorB, ColorHex }) b.BorderBrush = NormalBorder;
        ColorError.Visibility = Visibility.Collapsed; ColorPreview.Background = new SolidColorBrush(c);
        HueSurface.Fill = new SolidColorBrush(HsvToRgb(_hue, 1, 1));
        TextTarget.Background = new SolidColorBrush(SelectedTextColor); BackgroundTarget.Background = new SolidColorBrush(SelectedBackgroundColor);
        PositionMarkers(); _loading = false;
    }

    private void PositionMarkers() { if (SvMarker is null) return; Canvas.SetLeft(SvMarker, _saturation * SvCanvas.ActualWidth - 7); Canvas.SetTop(SvMarker, (1 - _value) * SvCanvas.ActualHeight - 7); Canvas.SetTop(HueMarker, _hue / 360 * HueCanvas.ActualHeight - 2); }
    private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { UpdateOpacity(); RaiseChanged(); }
    private void UpdateOpacity() { if (OpacityLabel is not null) OpacityLabel.Text = $"{OpacitySlider.Value:0}%"; }
    private void RaiseChanged() { if (!_loading) AppearanceChanged?.Invoke(this, new(SelectedTextColor, SelectedBackgroundColor, BackgroundOpacity)); }
    private static double Clamp01(double x) => Math.Clamp(x, 0, 1);
    private static Color Opaque(Color c) => Color.FromRgb(c.R, c.G, c.B);

    private static Color HsvToRgb(double h, double s, double v)
    {
        h = ((h % 360) + 360) % 360; var c = v * s; var x = c * (1 - Math.Abs(h / 60 % 2 - 1)); var m = v - c;
        var (r, g, b) = h switch { < 60 => (c, x, 0d), < 120 => (x, c, 0d), < 180 => (0d, c, x), < 240 => (0d, x, c), < 300 => (x, 0d, c), _ => (c, 0d, x) };
        return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
    }

    private static void RgbToHsv(Color c, out double h, out double s, out double v)
    {
        var r = c.R / 255d; var g = c.G / 255d; var b = c.B / 255d; var max = Math.Max(r, Math.Max(g, b)); var min = Math.Min(r, Math.Min(g, b)); var d = max - min;
        h = d == 0 ? 0 : max == r ? 60 * (((g - b) / d) % 6) : max == g ? 60 * ((b - r) / d + 2) : 60 * ((r - g) / d + 4);
        if (h < 0) h += 360; s = max == 0 ? 0 : d / max; v = max;
    }
}
