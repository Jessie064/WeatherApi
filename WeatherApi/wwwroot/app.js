/**
 * WeatherDash — app.js
 * Handles all frontend logic: fetching, rendering, theme, units, geolocation, recent cities
 */

'use strict';

// ── Constants ────────────────────────────────────────────
const API_BASE = '/api/weather';
const MAX_RECENT = 5;
const RECENT_KEY = 'wd_recent_cities';
const THEME_KEY = 'wd_theme';
const UNIT_KEY = 'wd_unit';

// ── State ─────────────────────────────────────────────────
let currentUnit = localStorage.getItem(UNIT_KEY) || 'metric';
let currentTheme = localStorage.getItem(THEME_KEY) || 'light';
let clockInterval = null;
let lastWeather = null;

// ── DOM refs ──────────────────────────────────────────────
const $ = id => document.getElementById(id);
const cityInput = $('cityInput');
const searchBtn = $('searchBtn');
const geoBtn = $('geoBtn');
const loader = $('loader');
const errorToast = $('errorToast');
const errorMsg = $('errorMsg');
const closeError = $('closeError');
const weatherDash = $('weatherDash');
const recentCities = $('recentCities');
const unitToggle = $('unitToggle');
const themeToggle = $('themeToggle');
const themeIcon = $('themeIcon');

// ── Init ──────────────────────────────────────────────────
(function init() {
  applyTheme(currentTheme);
  applyUnit(currentUnit);
  renderRecentCities();

  searchBtn.addEventListener('click', onSearch);
  cityInput.addEventListener('keydown', e => { if (e.key === 'Enter') onSearch(); });
  geoBtn.addEventListener('click', onGeolocate);
  closeError.addEventListener('click', hideError);
  unitToggle.addEventListener('click', onUnitToggle);
  themeToggle.addEventListener('click', onThemeToggle);
})();

// ── Search ────────────────────────────────────────────────
async function onSearch() {
  const city = cityInput.value.trim();
  if (!city) { showError('Please enter a city name.'); return; }
  await loadWeather(city);
}

async function loadWeather(city) {
  showLoader();
  hideError();
  try {
    const data = await fetchWeather(city, currentUnit);
    lastWeather = data;
    renderWeather(data);
    saveRecentCity(data.cityName);
    renderRecentCities();
    showDash();
  } catch (err) {
    showError(err.message || 'Something went wrong. Please try again.');
    hideDash();
  } finally {
    hideLoader();
  }
}

// ── Geolocation ───────────────────────────────────────────
function onGeolocate() {
  if (!navigator.geolocation) { showError('Geolocation is not supported by your browser.'); return; }
  showLoader();
  hideError();
  navigator.geolocation.getCurrentPosition(
    async pos => {
      try {
        const { latitude: lat, longitude: lon } = pos.coords;
        const data = await fetchWeatherByCoords(lat, lon, currentUnit);
        lastWeather = data;
        renderWeather(data);
        saveRecentCity(data.cityName);
        renderRecentCities();
        showDash();
      } catch (err) {
        showError(err.message);
        hideDash();
      } finally { hideLoader(); }
    },
    err => {
      hideLoader();
      showError('Unable to retrieve your location: ' + err.message);
    }
  );
}

// ── API calls ─────────────────────────────────────────────
async function fetchWeather(city, units) {
  const res = await fetch(`${API_BASE}?city=${encodeURIComponent(city)}&units=${units}`);
  return handleResponse(res);
}

async function fetchWeatherByCoords(lat, lon, units) {
  const res = await fetch(`${API_BASE}/geolocate?lat=${lat}&lon=${lon}&units=${units}`);
  return handleResponse(res);
}

async function handleResponse(res) {
  const data = await res.json();
  if (!res.ok) throw new Error(data.message || `Error ${res.status}`);
  return data;
}

// ── Render ────────────────────────────────────────────────
function renderWeather(d) {
  const unit = d.units === 'imperial' ? '°F' : '°C';
  const speed = d.units === 'imperial' ? 'mph' : 'm/s';

  $('cityName').textContent = d.cityName;
  $('countryBadge').textContent = d.country;
  $('mainTemp').textContent = `${Math.round(d.temperature)}${unit}`;
  $('weatherCondition').textContent = d.condition;
  $('weatherDesc').textContent = d.description;
  $('feelsLike').textContent = `${Math.round(d.feelsLike)}${unit}`;
  $('tempMax').textContent = `${Math.round(d.maxTemp)}${unit}`;
  $('tempMin').textContent = `${Math.round(d.minTemp)}${unit}`;
  $('humidity').textContent = `${d.humidity}%`;
  $('windSpeed').textContent = `${d.windSpeed} ${speed}`;
  $('visibility').textContent = d.visibility ? `${(d.visibility / 1000).toFixed(1)} km` : 'N/A';
  $('pressure').textContent = `${d.pressure} hPa`;
  $('sunrise').textContent = formatUnixTime(d.sunrise);
  $('sunset').textContent = formatUnixTime(d.sunset);

  // Weather icon
  $('weatherIcon').innerHTML = d.iconCode
    ? `<img src="https://openweathermap.org/img/wn/${d.iconCode}@2x.png" alt="${d.condition}" />`
    : '';

  // Start live clock for this city (approximate — use client time)
  startClock();

  // Render 5-day forecast
  renderForecast(d.forecastDays || [], d.units);
}

function renderForecast(days, units) {
  const strip = $('forecastStrip');
  const unit = units === 'imperial' ? '°F' : '°C';
  if (!days || days.length === 0) {
    strip.innerHTML = '<p style="color:var(--text-secondary);font-size:.85rem">No forecast data available.</p>';
    return;
  }
  strip.innerHTML = days.map(day => `
    <div class="forecast-card">
      <div class="fc-day">${day.dayName.substring(0, 3).toUpperCase()}<br/><span style="font-size:.7rem;font-weight:400;color:var(--text-secondary)">${formatDate(day.date)}</span></div>
      <div class="fc-icon">${day.iconCode ? `<img src="https://openweathermap.org/img/wn/${day.iconCode}@2x.png" alt="${day.condition}" />` : '🌡️'}</div>
      <div class="fc-cond">${day.condition}</div>
      <div class="fc-temps">
        <span class="max">${Math.round(day.maxTemp)}${unit}</span>
        <span style="opacity:.4"> / </span>
        <span class="min">${Math.round(day.minTemp)}${unit}</span>
      </div>
    </div>
  `).join('');
}

// ── Clock ─────────────────────────────────────────────────
function startClock() {
  if (clockInterval) clearInterval(clockInterval);
  updateClock();
  clockInterval = setInterval(updateClock, 1000);
}

function updateClock() {
  const now = new Date();
  const opts = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
  const timeStr = now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  $('dateTime').textContent = `${now.toLocaleDateString(undefined, opts)} · ${timeStr}`;
}

// ── Unit Toggle ───────────────────────────────────────────
function onUnitToggle() {
  currentUnit = currentUnit === 'metric' ? 'imperial' : 'metric';
  localStorage.setItem(UNIT_KEY, currentUnit);
  applyUnit(currentUnit);
  // Re-fetch if there's an active city
  const city = cityInput.value.trim() || (lastWeather ? lastWeather.cityName : '');
  if (city) loadWeather(city);
}

function applyUnit(unit) {
  unitToggle.textContent = unit === 'metric' ? '°C / °F' : '°F / °C';
}

// ── Theme Toggle ──────────────────────────────────────────
function onThemeToggle() {
  currentTheme = currentTheme === 'light' ? 'dark' : 'light';
  applyTheme(currentTheme);
  localStorage.setItem(THEME_KEY, currentTheme);
}

function applyTheme(theme) {
  document.documentElement.setAttribute('data-theme', theme);
  themeIcon.textContent = theme === 'light' ? '🌙' : '☀️';
}

// ── Recent Cities ─────────────────────────────────────────
function getRecentCities() {
  try { return JSON.parse(localStorage.getItem(RECENT_KEY)) || []; }
  catch { return []; }
}

function saveRecentCity(city) {
  let cities = getRecentCities().filter(c => c.toLowerCase() !== city.toLowerCase());
  cities.unshift(city);
  cities = cities.slice(0, MAX_RECENT);
  localStorage.setItem(RECENT_KEY, JSON.stringify(cities));
}

function removeRecentCity(city) {
  const cities = getRecentCities().filter(c => c.toLowerCase() !== city.toLowerCase());
  localStorage.setItem(RECENT_KEY, JSON.stringify(cities));
  renderRecentCities();
}

function renderRecentCities() {
  const cities = getRecentCities();
  if (cities.length === 0) { recentCities.innerHTML = ''; return; }
  recentCities.innerHTML = `
    <span style="font-size:.78rem;color:var(--text-secondary);align-self:center">Recent:</span>
    ${cities.map(c => `
      <span class="city-chip-wrap">
        <button class="city-chip" data-city="${c}" aria-label="Search ${c}">
          🕐 ${c}
        </button>
        <button type="button" class="chip-delete" data-city="${c}" aria-label="Remove ${c} from recent" title="Remove from recent"><svg width="12" height="12" viewBox="0 0 10 10" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><line x1="1" y1="1" x2="9" y2="9"/><line x1="9" y1="1" x2="1" y2="9"/></svg></button>
      </span>
    `).join('')}
  `;
  recentCities.querySelectorAll('.city-chip').forEach(chip => {
    chip.addEventListener('click', () => {
      cityInput.value = chip.dataset.city;
      loadWeather(chip.dataset.city);
    });
  });
  recentCities.querySelectorAll('.chip-delete').forEach(btn => {
    btn.addEventListener('click', (e) => {
      e.stopPropagation();
      removeRecentCity(btn.dataset.city);
    });
  });
}

// ── UI helpers ────────────────────────────────────────────
function showLoader() { loader.classList.remove('hidden'); }
function hideLoader() { loader.classList.add('hidden'); }
function showDash() { weatherDash.classList.remove('hidden'); }
function hideDash() { weatherDash.classList.add('hidden'); }
function hideError() { errorToast.classList.add('hidden'); }

function showError(msg) {
  errorMsg.textContent = msg;
  errorToast.classList.remove('hidden');
}

// ── Date helpers ──────────────────────────────────────────
function formatUnixTime(unix) {
  return new Date(unix * 1000).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function formatDate(dateStr) {
  // dateStr: "2026-03-06"
  const d = new Date(dateStr + 'T12:00:00');
  return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}
