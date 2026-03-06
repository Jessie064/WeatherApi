using System.Text.Json;
using WeatherApi.Models;

namespace WeatherApi.Services;

public class WeatherServiceException : Exception
{
    public int StatusCode { get; }
    public WeatherServiceException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}

public interface IWeatherService
{
    Task<WeatherResponse> GetWeatherByCityAsync(string city, string units = "metric");
    Task<WeatherResponse> GetWeatherByCoordinatesAsync(double lat, double lon, string units = "metric");
    Task<List<ForecastDay>> GetForecastAsync(string city, string units = "metric");
    Task<List<ForecastDay>> GetForecastByCoordinatesAsync(double lat, double lon, string units = "metric");
}

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public WeatherService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["OpenWeatherMap:ApiKey"] ?? "";
        _baseUrl = config["OpenWeatherMap:BaseUrl"] ?? "https://api.openweathermap.org/data/2.5";
    }

    public async Task<WeatherResponse> GetWeatherByCityAsync(string city, string units = "metric")
    {
        var url = $"{_baseUrl}/weather?q={Uri.EscapeDataString(city)}&units={units}&appid={_apiKey}";
        return await FetchCurrentWeatherAsync(url, units);
    }

    public async Task<WeatherResponse> GetWeatherByCoordinatesAsync(double lat, double lon, string units = "metric")
    {
        var url = $"{_baseUrl}/weather?lat={lat}&lon={lon}&units={units}&appid={_apiKey}";
        return await FetchCurrentWeatherAsync(url, units);
    }

    public async Task<List<ForecastDay>> GetForecastAsync(string city, string units = "metric")
    {
        var url = $"{_baseUrl}/forecast?q={Uri.EscapeDataString(city)}&units={units}&appid={_apiKey}";
        return await FetchForecastAsync(url);
    }

    public async Task<List<ForecastDay>> GetForecastByCoordinatesAsync(double lat, double lon, string units = "metric")
    {
        var url = $"{_baseUrl}/forecast?lat={lat}&lon={lon}&units={units}&appid={_apiKey}";
        return await FetchForecastAsync(url);
    }

    private async Task<WeatherResponse> FetchCurrentWeatherAsync(string url, string units)
    {
        HttpResponseMessage response;
        try { response = await _httpClient.GetAsync(url); }
        catch (Exception ex) { throw new WeatherServiceException($"Network error: {ex.Message}", 502); }

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            using var errDoc = JsonDocument.Parse(body);
            var msg = errDoc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : "Unknown error";
            int code = (int)response.StatusCode;
            if (code == 404) throw new WeatherServiceException($"City not found: {msg}", 404);
            if (code == 401) throw new WeatherServiceException("Invalid API key. Please update your OpenWeatherMap API key in appsettings.json.", 401);
            throw new WeatherServiceException($"Weather API error: {msg}", 502);
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var sys = root.GetProperty("sys");
        var wind = root.GetProperty("wind");
        var main = root.GetProperty("main");
        var weather = root.GetProperty("weather")[0];
        var coord = root.GetProperty("coord");

        return new WeatherResponse
        {
            CityName = root.GetProperty("name").GetString() ?? "",
            Country = sys.GetProperty("country").GetString() ?? "",
            Temperature = main.GetProperty("temp").GetDouble(),
            FeelsLike = main.GetProperty("feels_like").GetDouble(),
            MinTemp = main.GetProperty("temp_min").GetDouble(),
            MaxTemp = main.GetProperty("temp_max").GetDouble(),
            Humidity = main.GetProperty("humidity").GetInt32(),
            Pressure = main.GetProperty("pressure").GetInt32(),
            Condition = weather.GetProperty("main").GetString() ?? "",
            Description = weather.GetProperty("description").GetString() ?? "",
            IconCode = weather.GetProperty("icon").GetString() ?? "",
            WindSpeed = wind.GetProperty("speed").GetDouble(),
            Visibility = root.TryGetProperty("visibility", out var vis) ? vis.GetInt32() : 0,
            Sunrise = sys.GetProperty("sunrise").GetInt64(),
            Sunset = sys.GetProperty("sunset").GetInt64(),
            Lat = coord.GetProperty("lat").GetDouble(),
            Lon = coord.GetProperty("lon").GetDouble(),
            Units = units
        };
    }

    private async Task<List<ForecastDay>> FetchForecastAsync(string url)
    {
        HttpResponseMessage response;
        try { response = await _httpClient.GetAsync(url); }
        catch (Exception ex) { throw new WeatherServiceException($"Network error: {ex.Message}", 502); }

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            using var errDoc = JsonDocument.Parse(body);
            var msg = errDoc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : "Unknown error";
            int code = (int)response.StatusCode;
            if (code == 404) throw new WeatherServiceException($"City not found: {msg}", 404);
            throw new WeatherServiceException($"Forecast API error: {msg}", 502);
        }

        using var doc = JsonDocument.Parse(body);
        var list = doc.RootElement.GetProperty("list");

        // Group by day (take noon reading per day, skip today)
        var grouped = new Dictionary<string, JsonElement>();
        foreach (var item in list.EnumerateArray())
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64());
            var dateKey = dt.ToString("yyyy-MM-dd");
            var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
            if (dateKey == today) continue;
            if (!grouped.ContainsKey(dateKey) || dt.Hour == 12 || (dt.Hour < 12 && !grouped.ContainsKey(dateKey)))
                grouped[dateKey] = item;
        }

        var days = new List<ForecastDay>();
        foreach (var kvp in grouped.Take(5))
        {
            var item = kvp.Value;
            var dt = DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64());
            var main = item.GetProperty("main");
            var weather = item.GetProperty("weather")[0];
            var wind = item.GetProperty("wind");
            days.Add(new ForecastDay
            {
                Date = kvp.Key,
                DayName = dt.DayOfWeek.ToString(),
                MinTemp = main.GetProperty("temp_min").GetDouble(),
                MaxTemp = main.GetProperty("temp_max").GetDouble(),
                Condition = weather.GetProperty("main").GetString() ?? "",
                IconCode = weather.GetProperty("icon").GetString() ?? "",
                Humidity = main.GetProperty("humidity").GetInt32(),
                WindSpeed = wind.GetProperty("speed").GetDouble()
            });
        }
        return days;
    }
}
