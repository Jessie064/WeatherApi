using Microsoft.AspNetCore.Mvc;
using WeatherApi.Models;
using WeatherApi.Services;

namespace WeatherApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(IWeatherService weatherService, ILogger<WeatherController> logger)
    {
        _weatherService = weatherService;
        _logger = logger;
    }

    /// <summary>Get current weather for a city</summary>
    [HttpGet]
    public async Task<IActionResult> GetWeather([FromQuery] string city, [FromQuery] string units = "metric")
    {
        if (string.IsNullOrWhiteSpace(city))
            return BadRequest(new ErrorResponse { Message = "City name is required.", Code = 400 });

        try
        {
            var weather = await _weatherService.GetWeatherByCityAsync(city.Trim(), units);
            try
            {
                var forecast = await _weatherService.GetForecastAsync(city.Trim(), units);
                weather.ForecastDays = forecast;
            }
            catch { /* forecast failure is non-fatal */ }
            return Ok(weather);
        }
        catch (WeatherServiceException ex)
        {
            _logger.LogWarning("WeatherService error for city '{City}': {Message}", city, ex.Message);
            return StatusCode(ex.StatusCode, new ErrorResponse { Message = ex.Message, Code = ex.StatusCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching weather for '{City}'", city);
            return StatusCode(502, new ErrorResponse { Message = "An unexpected error occurred.", Code = 502 });
        }
    }

    /// <summary>Get current weather by geographic coordinates (geolocation)</summary>
    [HttpGet("geolocate")]
    public async Task<IActionResult> GetWeatherByCoords([FromQuery] double lat, [FromQuery] double lon, [FromQuery] string units = "metric")
    {
        try
        {
            var weather = await _weatherService.GetWeatherByCoordinatesAsync(lat, lon, units);
            try
            {
                var forecast = await _weatherService.GetForecastByCoordinatesAsync(lat, lon, units);
                weather.ForecastDays = forecast;
            }
            catch { /* non-fatal */ }
            return Ok(weather);
        }
        catch (WeatherServiceException ex)
        {
            return StatusCode(ex.StatusCode, new ErrorResponse { Message = ex.Message, Code = ex.StatusCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching weather for coords {Lat},{Lon}", lat, lon);
            return StatusCode(502, new ErrorResponse { Message = "An unexpected error occurred.", Code = 502 });
        }
    }

    /// <summary>Get 5-day forecast for a city</summary>
    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecast([FromQuery] string city, [FromQuery] string units = "metric")
    {
        if (string.IsNullOrWhiteSpace(city))
            return BadRequest(new ErrorResponse { Message = "City name is required.", Code = 400 });

        try
        {
            var forecast = await _weatherService.GetForecastAsync(city.Trim(), units);
            return Ok(forecast);
        }
        catch (WeatherServiceException ex)
        {
            return StatusCode(ex.StatusCode, new ErrorResponse { Message = ex.Message, Code = ex.StatusCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching forecast for '{City}'", city);
            return StatusCode(502, new ErrorResponse { Message = "An unexpected error occurred.", Code = 502 });
        }
    }
}
