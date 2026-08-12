using System.Windows.Controls;
using System.Windows.Threading;
using WinWidget.Models;
using WinWidget.Services;

namespace WinWidget.Views;

public partial class WeatherWidgetView : UserControl
{
    private readonly WidgetSettings _settings;
    private readonly WeatherService _service;
    private readonly Action _cacheChanged;
    private CancellationTokenSource? _refreshCancellation;
    private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromMinutes(30) };

    public WeatherWidgetView(WidgetSettings settings, WeatherService service, Action cacheChanged)
    {
        InitializeComponent();
        _settings = settings;
        _service = service;
        _cacheChanged = cacheChanged;
        _refreshTimer.Tick += async (_, _) => await RefreshAsync();
        Loaded += OnLoaded;
        Unloaded += (_, _) => { _refreshTimer.Stop(); CancelRefresh(); };
        ShowCache(offline: false);
    }

    public async Task RefreshAsync()
    {
        CancelRefresh();
        var request = new CancellationTokenSource();
        _refreshCancellation = request;
        StatusLabel.Text = "Обновление…";
        try
        {
            var snapshot = await _service.GetWeatherAsync(_settings.Location, request.Token);
            if (!ReferenceEquals(_refreshCancellation, request) || request.IsCancellationRequested) return;
            _settings.WeatherCache = new WeatherCache
            {
                Location = snapshot.Location, Temperature = snapshot.Temperature,
                MinimumTemperature = snapshot.MinimumTemperature, MaximumTemperature = snapshot.MaximumTemperature,
                WeatherCode = snapshot.WeatherCode, UpdatedAt = snapshot.UpdatedAt
            };
            _cacheChanged();
            ShowCache(offline: false);
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested) { }
        catch (Exception exception) when (!IsFatal(exception))
        {
            if (ReferenceEquals(_refreshCancellation, request))
                ShowCache(offline: true, exception.Message);
        }
        finally
        {
            if (ReferenceEquals(_refreshCancellation, request)) _refreshCancellation = null;
            request.Dispose();
        }
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _refreshTimer.Start();
        await RefreshAsync();
    }

    private void ShowCache(bool offline, string? error = null)
    {
        var cache = _settings.WeatherCache;
        LocationLabel.Text = cache?.Location ?? _settings.Location;
        if (cache is null)
        {
            TemperatureLabel.Text = "—°";
            ConditionLabel.Text = offline ? "Нет подключения" : "Загрузка погоды";
            RangeLabel.Text = string.Empty;
            WeatherIcon.Text = "☁";
            StatusLabel.Text = offline ? ShortError(error) : string.Empty;
            return;
        }
        TemperatureLabel.Text = $"{Math.Round(cache.Temperature):0}°";
        ConditionLabel.Text = Describe(cache.WeatherCode);
        RangeLabel.Text = $"↓ {Math.Round(cache.MinimumTemperature):0}°  ↑ {Math.Round(cache.MaximumTemperature):0}°";
        WeatherIcon.Text = Icon(cache.WeatherCode);
        StatusLabel.Text = offline
            ? $"Офлайн · данные {cache.UpdatedAt.LocalDateTime:g}"
            : $"Обновлено {cache.UpdatedAt.LocalDateTime:t}";
    }

    private void CancelRefresh()
    {
        var request = _refreshCancellation;
        _refreshCancellation = null;
        request?.Cancel();
    }
    private static bool IsFatal(Exception exception) =>
        exception is OutOfMemoryException or StackOverflowException or AccessViolationException;
    private static string ShortError(string? value) => string.IsNullOrWhiteSpace(value) ? "Не удалось загрузить погоду" : value.Length <= 50 ? value : value[..50] + "…";
    private static string Icon(int code) => code switch { 0 => "☀", 1 or 2 => "🌤", 3 => "☁", 45 or 48 => "≋", >= 51 and <= 67 => "☂", >= 71 and <= 77 => "❄", >= 80 and <= 82 => "☂", >= 95 => "⚡", _ => "☁" };
    private static string Describe(int code) => code switch { 0 => "Ясно", 1 => "В основном ясно", 2 => "Переменная облачность", 3 => "Пасмурно", 45 or 48 => "Туман", >= 51 and <= 57 => "Морось", >= 61 and <= 67 => "Дождь", >= 71 and <= 77 => "Снег", >= 80 and <= 82 => "Ливень", >= 85 and <= 86 => "Снегопад", >= 95 => "Гроза", _ => "Погода" };
}
