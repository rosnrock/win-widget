using System.IO;
using System.Security;
using System.Text.Json;
using System.Text.RegularExpressions;
using WinWidget.Models;

namespace WinWidget.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly Regex HexColor = new("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant);
    private readonly string _settingsPath;

    public SettingsService(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinWidget", "settings.json");
    }

    public ApplicationSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                var defaults = new ApplicationSettings();
                Migrate(defaults);
                return defaults;
            }
            var settings = JsonSerializer.Deserialize<ApplicationSettings>(File.ReadAllText(_settingsPath), JsonOptions)
                           ?? new ApplicationSettings();
            Migrate(settings);
            return settings;
        }
        catch (JsonException)
        {
            return new ApplicationSettings();
        }
        catch (IOException)
        {
            return new ApplicationSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new ApplicationSettings();
        }
        catch (SecurityException)
        {
            return new ApplicationSettings();
        }
    }

    private static void Migrate(ApplicationSettings settings)
    {
        var sourceSchemaVersion = settings.SchemaVersion;
        settings.Widgets ??= [];
        if (!double.IsFinite(settings.GridSize) || settings.GridSize < 4) settings.GridSize = 16;
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var counters = new Dictionary<WidgetKind, int>();
        var migratedWidgets = new List<WidgetSettings>(settings.Widgets.Count);

        foreach (var widget in settings.Widgets)
        {
            if (widget is null || !Enum.IsDefined(widget.Kind)) continue;

            if (string.IsNullOrWhiteSpace(widget.Id) || !ids.Add(widget.Id))
            {
                do { widget.Id = Guid.NewGuid().ToString("N"); }
                while (!ids.Add(widget.Id));
            }

            counters.TryGetValue(widget.Kind, out var count);
            count++;
            counters[widget.Kind] = count;
            if (string.IsNullOrWhiteSpace(widget.DisplayName))
                widget.DisplayName = GetDefaultDisplayName(widget.Kind, count);

            widget.TextColor = NormalizeColor(widget.TextColor, "#23478B");
            widget.BackgroundColor = NormalizeColor(widget.BackgroundColor, "#FFFFFF");
            widget.Text ??= string.Empty;
            widget.ImagePath ??= string.Empty;
            widget.Location = string.IsNullOrWhiteSpace(widget.Location) ? "Moscow" : widget.Location.Trim();
            if (string.Equals(widget.Location, "Москва", StringComparison.OrdinalIgnoreCase))
            {
                widget.Location = "Moscow";
                // The cached place name remains visible while offline. Discard
                // the legacy Russian cache so the next refresh resolves it in
                // English together with the migrated configured location.
                widget.WeatherCache = null;
            }
            widget.BackgroundOpacity = double.IsFinite(widget.BackgroundOpacity)
                ? Math.Clamp(widget.BackgroundOpacity, 0, 1)
                : 0;
            if (sourceSchemaVersion < 4 && widget.Kind == WidgetKind.Weather)
            {
                // v4 aligns the weather card with the clock card. Migrate the old
                // 320 x 240 weather geometry once while keeping its screen position.
                widget.Width = 440;
                widget.Height = 210;
            }
            if (sourceSchemaVersion < 5 && widget.Kind == WidgetKind.Weather)
            {
                // Keep the established card width, but make weather exactly two
                // thirds of the 220 px calendar height.
                widget.Width = 440;
                widget.Height = 220d * 2d / 3d;
            }
            if (sourceSchemaVersion < 6 && widget.Kind == WidgetKind.Image)
            {
                // Image widgets use the same footprint as calendar cards.
                widget.Width = 230;
                widget.Height = 220;
            }
            NormalizeGeometry(widget);
            migratedWidgets.Add(widget);
        }

        settings.Widgets = migratedWidgets;
        settings.SchemaVersion = ApplicationSettings.CurrentSchemaVersion;
    }

    private static void NormalizeGeometry(WidgetSettings widget)
    {
        var (defaultLeft, defaultTop, defaultWidth, defaultHeight, minimumWidth, minimumHeight) = widget.Kind switch
        {
            WidgetKind.Clock => (440d, 80d, 440d, 210d, 240d, 120d),
            WidgetKind.Calendar => (70d, 170d, 230d, 220d, 180d, 160d),
            WidgetKind.Notes => (70d, 420d, 300d, 170d, 220d, 92d),
            WidgetKind.Weather => (340d, 320d, 440d, 220d * 2d / 3d, 440d, 220d * 2d / 3d),
            WidgetKind.Image => (340d, 320d, 230d, 220d, 180d, 160d),
            _ => (80d, 80d, 320d, 180d, 120d, 80d)
        };

        widget.Left = NormalizeCoordinate(widget.Left, defaultLeft);
        widget.Top = NormalizeCoordinate(widget.Top, defaultTop);
        widget.Width = NormalizeSize(widget.Width, defaultWidth, minimumWidth, 4000);
        widget.Height = NormalizeSize(widget.Height, defaultHeight, minimumHeight, 4000);
    }

    private static double NormalizeCoordinate(double value, double fallback) =>
        double.IsFinite(value) && value is >= -100000 and <= 100000 ? value : fallback;

    private static double NormalizeSize(double value, double fallback, double minimum, double maximum) =>
        double.IsFinite(value) && value >= minimum ? Math.Min(value, maximum) : fallback;

    private static string NormalizeColor(string? value, string fallback) =>
        value is not null && HexColor.IsMatch(value) ? value.ToUpperInvariant() : fallback;

    private static string GetDefaultDisplayName(WidgetKind kind, int number)
    {
        var baseName = kind switch
        {
            WidgetKind.Clock => "Часы",
            WidgetKind.Calendar => "Календарь",
            WidgetKind.Notes => "Заметка",
            WidgetKind.Weather => "Погода",
            WidgetKind.Image => "Image",
            _ => "Виджет"
        };
        return number == 1 ? baseName : $"{baseName} {number}";
    }

    public void Save(ApplicationSettings settings)
    {
        var temporaryPath = _settingsPath + ".tmp";
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporaryPath, _settingsPath, true);
        }
        catch (IOException)
        {
            // A transient filesystem failure must not terminate the desktop process.
        }
        catch (UnauthorizedAccessException)
        {
            // Keep the current in-memory settings when the profile is not writable.
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
