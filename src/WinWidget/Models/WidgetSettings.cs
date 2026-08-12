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
    public string Location { get; set; } = "Москва";
    public WeatherCache? WeatherCache { get; set; }
}

public sealed class WeatherCache
{
    public string Location { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double MinimumTemperature { get; set; }
    public double MaximumTemperature { get; set; }
    public int WeatherCode { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
