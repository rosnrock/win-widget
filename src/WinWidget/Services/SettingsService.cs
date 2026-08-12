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
            if (!File.Exists(_settingsPath)) return new ApplicationSettings();
            return JsonSerializer.Deserialize<ApplicationSettings>(File.ReadAllText(_settingsPath), JsonOptions)
                   ?? new ApplicationSettings();
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
