using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinWidget.Views;

public sealed class AppearanceChangedEventArgs(Color textColor, Color backgroundColor, double backgroundOpacity)
    : EventArgs
{
    public Color TextColor { get; } = textColor;
    public Color BackgroundColor { get; } = backgroundColor;
    public double BackgroundOpacity { get; } = backgroundOpacity;
}

public partial class AppearancePanel : UserControl
{
    private static readonly Brush NormalBorder = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
    private static readonly Brush InvalidBorder = new SolidColorBrush(Color.FromRgb(198, 40, 40));
    private bool _isLoading;

    public AppearancePanel()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateUi();
    }

    public event EventHandler<AppearanceChangedEventArgs>? AppearanceChanged;

    public Color SelectedTextColor { get; private set; } = Color.FromRgb(35, 71, 139);
    public Color SelectedBackgroundColor { get; private set; } = Colors.White;
    public double BackgroundOpacity => OpacitySlider.Value / 100d;

    public string Title { get => PanelTitle.Text; set => PanelTitle.Text = value; }

    public void SetAppearance(Color textColor, Color backgroundColor, double backgroundOpacity)
    {
        _isLoading = true;
        try
        {
            SelectedTextColor = Opaque(textColor);
            SelectedBackgroundColor = Opaque(backgroundColor);
            OpacitySlider.Value = Math.Clamp(backgroundOpacity, 0, 1) * 100d;
            UpdateUi();
        }
        finally { _isLoading = false; }
    }

    private void OnTextColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && TryGetSwatchColor(button, out var color))
            SetTextColor(color);
    }

    private void OnBackgroundColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && TryGetSwatchColor(button, out var color))
            SetBackgroundColor(color);
    }

    private void OnTextEditorChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || TextR is null || TextG is null || TextB is null || TextHex is null || TextError is null) return;
        if (ReferenceEquals(sender, TextHex))
        {
            if (TryParseHex(TextHex.Text, out var color)) SetTextColor(color);
            else ShowHexError(TextHex, TextError);
            return;
        }

        if (TryParseRgb(TextR, TextG, TextB, TextError, out var rgb)) SetTextColor(rgb);
    }

    private void OnBackgroundEditorChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || BackgroundR is null || BackgroundG is null || BackgroundB is null ||
            BackgroundHex is null || BackgroundError is null) return;
        if (ReferenceEquals(sender, BackgroundHex))
        {
            if (TryParseHex(BackgroundHex.Text, out var color)) SetBackgroundColor(color);
            else ShowHexError(BackgroundHex, BackgroundError);
            return;
        }

        if (TryParseRgb(BackgroundR, BackgroundG, BackgroundB, BackgroundError, out var rgb))
            SetBackgroundColor(rgb);
    }

    private void SetTextColor(Color color)
    {
        SelectedTextColor = Opaque(color);
        SyncEditor(TextR, TextG, TextB, TextHex, TextError, SelectedTextColor);
        UpdateSelectionRings(TextPalette, SelectedTextColor);
        TextPreview.Background = new SolidColorBrush(SelectedTextColor);
        RaiseAppearanceChanged();
    }

    private void SetBackgroundColor(Color color)
    {
        SelectedBackgroundColor = Opaque(color);
        SyncEditor(BackgroundR, BackgroundG, BackgroundB, BackgroundHex, BackgroundError, SelectedBackgroundColor);
        UpdateSelectionRings(BackgroundPalette, SelectedBackgroundColor);
        BackgroundPreview.Background = new SolidColorBrush(SelectedBackgroundColor);
        RaiseAppearanceChanged();
    }

    private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityLabel is null) return;
        OpacityLabel.Text = $"{e.NewValue:0}%";
        RaiseAppearanceChanged();
    }

    private void UpdateUi()
    {
        if (TextR is null || TextG is null || TextB is null || TextHex is null || TextError is null ||
            BackgroundR is null || BackgroundG is null || BackgroundB is null || BackgroundHex is null ||
            BackgroundError is null || TextPreview is null || BackgroundPreview is null || OpacityLabel is null) return;
        var wasLoading = _isLoading;
        _isLoading = true;
        SyncEditor(TextR, TextG, TextB, TextHex, TextError, SelectedTextColor);
        SyncEditor(BackgroundR, BackgroundG, BackgroundB, BackgroundHex, BackgroundError, SelectedBackgroundColor);
        TextPreview.Background = new SolidColorBrush(SelectedTextColor);
        BackgroundPreview.Background = new SolidColorBrush(SelectedBackgroundColor);
        OpacityLabel.Text = $"{OpacitySlider.Value:0}%";
        UpdateSelectionRings(TextPalette, SelectedTextColor);
        UpdateSelectionRings(BackgroundPalette, SelectedBackgroundColor);
        _isLoading = wasLoading;
    }

    private void SyncEditor(TextBox red, TextBox green, TextBox blue, TextBox hex, TextBlock error, Color color)
    {
        var wasLoading = _isLoading;
        _isLoading = true;
        red.Text = color.R.ToString(CultureInfo.InvariantCulture);
        green.Text = color.G.ToString(CultureInfo.InvariantCulture);
        blue.Text = color.B.ToString(CultureInfo.InvariantCulture);
        hex.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        foreach (var box in new[] { red, green, blue, hex }) box.BorderBrush = NormalBorder;
        error.Visibility = Visibility.Collapsed;
        _isLoading = wasLoading;
    }

    private static bool TryParseRgb(TextBox red, TextBox green, TextBox blue, TextBlock error, out Color color)
    {
        var validR = TryByte(red.Text, out var r);
        var validG = TryByte(green.Text, out var g);
        var validB = TryByte(blue.Text, out var b);
        red.BorderBrush = validR ? NormalBorder : InvalidBorder;
        green.BorderBrush = validG ? NormalBorder : InvalidBorder;
        blue.BorderBrush = validB ? NormalBorder : InvalidBorder;
        error.Text = "Enter a whole number from 0 to 255";
        error.Visibility = validR && validG && validB ? Visibility.Collapsed : Visibility.Visible;
        color = validR && validG && validB ? Color.FromRgb(r, g, b) : default;
        return validR && validG && validB;
    }

    private static bool TryByte(string value, out byte result) =>
        byte.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result);

    private static bool TryParseHex(string value, out Color color)
    {
        var hex = value.Trim();
        if (hex.StartsWith('#')) hex = hex[1..];
        if (hex.Length == 6 && uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            color = Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
            return true;
        }
        color = default;
        return false;
    }

    private static void ShowHexError(TextBox box, TextBlock error)
    {
        box.BorderBrush = InvalidBorder;
        error.Text = "Use HEX format #RRGGBB";
        error.Visibility = Visibility.Visible;
    }

    private void RaiseAppearanceChanged()
    {
        if (!_isLoading)
            AppearanceChanged?.Invoke(this, new(SelectedTextColor, SelectedBackgroundColor, BackgroundOpacity));
    }

    private static void UpdateSelectionRings(ItemsControl palette, Color selected)
    {
        foreach (var button in palette.Items.OfType<Button>())
        {
            var isSelected = TryGetSwatchColor(button, out var color) && color == Opaque(selected);
            button.BorderBrush = isSelected ? new SolidColorBrush(Color.FromRgb(35, 71, 139)) : NormalBorder;
            button.BorderThickness = new Thickness(isSelected ? 3 : 1);
        }
    }

    private static bool TryGetSwatchColor(FrameworkElement element, out Color color)
    {
        try
        {
            color = element.Tag is string value ? Opaque((Color)ColorConverter.ConvertFromString(value)) : default;
            return element.Tag is string;
        }
        catch { color = default; return false; }
    }

    private static Color Opaque(Color color) => Color.FromRgb(color.R, color.G, color.B);
}
