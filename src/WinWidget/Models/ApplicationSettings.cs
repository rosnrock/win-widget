namespace WinWidget.Models;

public sealed class ApplicationSettings
{
    public int SchemaVersion { get; set; } = 1;
    public List<WidgetSettings> Widgets { get; set; } =
    [
        new() { Kind = WidgetKind.Clock, Left = 440, Top = 80, Width = 440, Height = 210 },
        new() { Kind = WidgetKind.Calendar, Left = 70, Top = 170, Width = 230, Height = 220 },
        new() { Kind = WidgetKind.Notes, Left = 70, Top = 420, Width = 300, Height = 170 }
    ];
}
