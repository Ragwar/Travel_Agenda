using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TravelAgenda.Services.Interfaces;
using TravelAgenda.Models;
using TravelAgenda.Services.Models;

namespace TravelAgenda.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public WeatherService(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _apiKey = cfg["WeatherAPI:ApiKey"]
                      ?? throw new ArgumentNullException("WeatherAPI:ApiKey not set");
        }

        public async Task<WeatherForecastResponse> GetForecastAsync(string locationQuery, int days)
        {
            // limit days to WeatherAPI.com maximum (365 on paid plans; adjust if you need)
            days = Math.Min(Math.Max(days, 1), 14);

            var url = $"https://api.weatherapi.com/v1/forecast.json" +
                      $"?key={_apiKey}" +
                      $"&q={Uri.EscapeDataString(locationQuery)}" +
                      $"&days={days}" +
                      $"&aqi=no&alerts=no";

            return await _http.GetFromJsonAsync<WeatherForecastResponse>(url);
        }
    }
}
