namespace WeatherApi.Models;

public class WeatherResponse
{
    public string CityName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public double MinTemp { get; set; }
    public double MaxTemp { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconCode { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
    public int Visibility { get; set; }
    public int Pressure { get; set; }
    public long Sunrise { get; set; }
    public long Sunset { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string Units { get; set; } = "metric";
    public List<ForecastDay> ForecastDays { get; set; } = new();
}

public class ForecastDay
{
    public string Date { get; set; } = string.Empty;
    public string DayName { get; set; } = string.Empty;
    public double MinTemp { get; set; }
    public double MaxTemp { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string IconCode { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public double WindSpeed { get; set; }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public int Code { get; set; }
}
