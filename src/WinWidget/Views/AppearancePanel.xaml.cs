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
    private bool _isLoading;

    public AppearancePanel()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateSelectionRings();
    }

    public event EventHandler<AppearanceChangedEventArgs>? AppearanceChanged;

    public Color SelectedTextColor { get; private set; } = Color.FromRgb(35, 71, 139);
    public Color SelectedBackgroundColor { get; private set; } = Colors.White;
    public double BackgroundOpacity => OpacitySlider.Value / 100d;

    public string Title
    {
        get => PanelTitle.Text;
        set => PanelTitle.Text = value;
    }

    public void SetAppearance(Color textColor, Color backgroundColor, double backgroundOpacity)
    {
        _isLoading = true;
        try
        {
            SelectedTextColor = Opaque(textColor);
            SelectedBackgroundColor = Opaque(backgroundColor);
            OpacitySlider.Value = Math.Clamp(backgroundOpacity, 0, 1) * 100d;
            OpacityLabel.Text = $"{OpacitySlider.Value:0}%";
            UpdateSelectionRings();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void OnTextColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !TryGetColor(button, out var color)) return;
        SelectedTextColor = color;
        UpdateSelectionRings();
        RaiseAppearanceChanged();
    }

    private void OnBackgroundColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !TryGetColor(button, out var color)) return;
        SelectedBackgroundColor = color;
        UpdateSelectionRings();
        RaiseAppearanceChanged();
    }

    private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityLabel is null) return;
        OpacityLabel.Text = $"{e.NewValue:0}%";
        RaiseAppearanceChanged();
    }

    private void RaiseAppearanceChanged()
    {
        if (_isLoading) return;
        AppearanceChanged?.Invoke(this,
            new AppearanceChangedEventArgs(SelectedTextColor, SelectedBackgroundColor, BackgroundOpacity));
    }

    private void UpdateSelectionRings()
    {
        if (TextPalette is null || BackgroundPalette is null) return;
        UpdateSelectionRings(TextPalette, SelectedTextColor);
        UpdateSelectionRings(BackgroundPalette, SelectedBackgroundColor);
    }

    private static void UpdateSelectionRings(ItemsControl palette, Color selected)
    {
        foreach (var button in palette.Items.OfType<Button>())
        {
            var isSelected = TryGetColor(button, out var color) && color == Opaque(selected);
            button.BorderBrush = isSelected
                ? new SolidColorBrush(Color.FromRgb(35, 71, 139))
                : new SolidColorBrush(Color.FromArgb(32, 0, 0, 0));
            button.BorderThickness = new Thickness(isSelected ? 3 : 1);
        }
    }

    private static bool TryGetColor(FrameworkElement element, out Color color)
    {
        try
        {
            color = element.Tag is string value
                ? Opaque((Color)ColorConverter.ConvertFromString(value))
                : default;
            return element.Tag is string;
        }
        catch (FormatException)
        {
            color = default;
            return false;
        }
    }

    private static Color Opaque(Color color) => Color.FromRgb(color.R, color.G, color.B);
}
