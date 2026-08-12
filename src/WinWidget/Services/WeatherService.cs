using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace WinWidget.Services;

public sealed record WeatherSnapshot(string Location, double Temperature, double MinimumTemperature,
    double MaximumTemperature, int WeatherCode, DateTimeOffset UpdatedAt);

public sealed class WeatherService
{
    private static readonly HttpClient Client = CreateClient();

    public async Task<WeatherSnapshot> GetWeatherAsync(string location, CancellationToken cancellationToken)
    {
        location = string.IsNullOrWhiteSpace(location) ? "Moscow" : location.Trim();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(12));

        var geocodeUrl = "https://geocoding-api.open-meteo.com/v1/search?count=1&language=en&format=json&name=" +
                         Uri.EscapeDataString(location);
        using var geocodeResponse = await Client.GetAsync(geocodeUrl, timeout.Token).ConfigureAwait(false);
        geocodeResponse.EnsureSuccessStatusCode();
        await using var geocodeStream = await geocodeResponse.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        using var geocode = await JsonDocument.ParseAsync(geocodeStream, cancellationToken: timeout.Token).ConfigureAwait(false);
        var results = geocode.RootElement.TryGetProperty("results", out var resultArray) ? resultArray : default;
        if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
            throw new InvalidOperationException("City not found");

        var place = results[0];
        var latitude = place.GetProperty("latitude").GetDouble().ToString(CultureInfo.InvariantCulture);
        var longitude = place.GetProperty("longitude").GetDouble().ToString(CultureInfo.InvariantCulture);
        var resolvedName = place.TryGetProperty("name", out var name) ? name.GetString() ?? location : location;
        var forecastUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}" +
                          "&current=temperature_2m,weather_code&daily=temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=1";
        using var forecastResponse = await Client.GetAsync(forecastUrl, timeout.Token).ConfigureAwait(false);
        forecastResponse.EnsureSuccessStatusCode();
        await using var forecastStream = await forecastResponse.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        using var forecast = await JsonDocument.ParseAsync(forecastStream, cancellationToken: timeout.Token).ConfigureAwait(false);
        var current = forecast.RootElement.GetProperty("current");
        var daily = forecast.RootElement.GetProperty("daily");
        return new WeatherSnapshot(resolvedName, current.GetProperty("temperature_2m").GetDouble(),
            daily.GetProperty("temperature_2m_min")[0].GetDouble(), daily.GetProperty("temperature_2m_max")[0].GetDouble(),
            current.GetProperty("weather_code").GetInt32(), DateTimeOffset.Now);
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("WinWidget/1.0 (Windows 11 desktop widget)");
        return client;
    }
}
