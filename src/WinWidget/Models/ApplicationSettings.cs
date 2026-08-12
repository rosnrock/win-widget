namespace WinWidget.Models;

public sealed class ApplicationSettings
{
    public const int CurrentSchemaVersion = 4;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public bool SnapToGrid { get; set; }
    public double GridSize { get; set; } = 16;
    public List<WidgetSettings> Widgets { get; set; } =
    [
        new() { Kind = WidgetKind.Clock, DisplayName = "Часы", Left = 440, Top = 80, Width = 440, Height = 210 },
        new() { Kind = WidgetKind.Calendar, DisplayName = "Календарь", Left = 70, Top = 170, Width = 230, Height = 220 },
        new() { Kind = WidgetKind.Notes, DisplayName = "Заметка", Left = 70, Top = 420, Width = 300, Height = 170 }
    ];
}
