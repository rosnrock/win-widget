namespace WinWidget.Models;

public sealed class WidgetSettings
{
    public WidgetKind Kind { get; set; }
    public double Left { get; set; } = 80;
    public double Top { get; set; } = 80;
    public double Width { get; set; } = 320;
    public double Height { get; set; } = 180;
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string TextColor { get; set; } = "#23478B";
    public double BackgroundOpacity { get; set; }
    public bool IsLocked { get; set; }
    public bool IsAlwaysOnTop { get; set; }
    public string Text { get; set; } = string.Empty;
}
