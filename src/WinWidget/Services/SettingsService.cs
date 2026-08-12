using System.IO;
using System.Text.Json;
using WinWidget.Models;

namespace WinWidget.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
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
    }

    private static void Migrate(ApplicationSettings settings)
    {
        settings.Widgets ??= [];
        if (!double.IsFinite(settings.GridSize) || settings.GridSize < 4) settings.GridSize = 16;
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var counters = new Dictionary<WidgetKind, int>();

        foreach (var widget in settings.Widgets)
        {
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
        }

        settings.SchemaVersion = ApplicationSettings.CurrentSchemaVersion;
    }

    private static string GetDefaultDisplayName(WidgetKind kind, int number)
    {
        var baseName = kind switch
        {
            WidgetKind.Clock => "Часы",
            WidgetKind.Calendar => "Календарь",
            WidgetKind.Notes => "Заметка",
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
