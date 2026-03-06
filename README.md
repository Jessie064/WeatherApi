# 🌤️ Gero WeatherSys

A real-time weather dashboard built with **ASP.NET Core** (C#) on the backend and **Vanilla HTML/CSS/JS** on the frontend. Search any city in the world to get live weather conditions, stats, and a 5-day forecast.

---

## 📸 Features

- 🔍 **City Search** — Search any city by name
- 📍 **Geolocation** — Use your current GPS location
- 🌡️ **Unit Toggle** — Switch between °C and °F
- 🌙 **Dark / Light Theme** — Persisted across sessions
- 🕐 **Recent Cities** — Last 5 searches saved locally, with delete support
- 📅 **5-Day Forecast** — Daily high/low with weather icons
- 💧 **Detailed Stats** — Humidity, wind speed, visibility, pressure, sunrise & sunset

---

## 🏗️ Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core (.NET 10), C# |
| HTTP Client | `IHttpClientFactory` |
| Frontend | HTML5, CSS3, Vanilla JavaScript |
| Weather Data | [OpenWeatherMap API](https://openweathermap.org/) |
| Storage | Browser `localStorage` (theme, unit, recent cities) |

---

## 📁 Project Structure

```
WeatherApi/
├── appsettings.json          # API key & configuration
├── Program.cs                # App entry point & service wiring
├── WeatherApi.csproj         # Project dependencies
│
├── Models/
│   └── WeatherModels.cs      # WeatherResponse, ForecastDay, ErrorResponse
│
├── Services/
│   └── WeatherService.cs     # Fetches & parses OpenWeatherMap data
│
├── Controllers/
│   └── WeatherController.cs  # REST API endpoints
│
└── wwwroot/                  # Static frontend files
    ├── index.html            # Page structure
    ├── styles.css            # Glassmorphism UI & theming
    └── app.js                # All frontend logic
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An [OpenWeatherMap API key](https://openweathermap.org/api) (free tier works)

### Setup

1. **Clone the repository**
   ```bash
   git clone <your-repo-url>
   cd weather/WeatherApi
   ```

2. **Add your API key** in `appsettings.json`:
   ```json
   "OpenWeatherMap": {
     "ApiKey": "YOUR_API_KEY_HERE",
     "BaseUrl": "https://api.openweathermap.org/data/2.5"
   }
   ```

3. **Run the app**
   ```bash
   dotnet run
   ```

4. **Open your browser** and go to:
   ```
   http://localhost:5000
   ```

---

## 🌐 API Endpoints

| Method | URL | Description |
|---|---|---|
| GET | `/api/weather?city=Manila` | Current weather by city name |
| GET | `/api/weather?city=Manila&units=imperial` | Weather in °F |
| GET | `/api/weather/geolocate?lat=14.6&lon=121.0` | Weather by coordinates |
| GET | `/api/weather/forecast?city=Manila` | 5-day forecast only |

---

## ⚙️ Configuration

| Key | Description |
|---|---|
| `OpenWeatherMap:ApiKey` | Your OpenWeatherMap API key |
| `OpenWeatherMap:BaseUrl` | API base URL (default: `https://api.openweathermap.org/data/2.5`) |
| `Urls` | Server listening addresses (default: `http://localhost:5000`) |

---

## 📝 Notes

- If you see an **"address already in use"** error on startup, a previous instance is still running. Find and kill it:
  ```powershell
  netstat -ano | findstr :5000
  taskkill /PID <PID> /F
  ```
- The OpenWeatherMap free API key may take up to **a few hours** to activate after registration.

---

## 📄 License

MIT — free to use and modify.
