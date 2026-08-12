namespace WinWidget.Models;

public sealed class WidgetSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public WidgetKind Kind { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
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
