using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TravelAgenda.Services.Models
{
    public class WeatherForecastResponse
    {
        public Location Location { get; set; }
        public Current Current { get; set; }
        public Forecast Forecast { get; set; }
    }

    public class Location
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("region")] public string Region { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("lat")] public decimal Lat { get; set; }
        [JsonPropertyName("lon")] public decimal Lon { get; set; }
        [JsonPropertyName("localtime")] public string LocalTime { get; set; }
    }

    public class Current
    {
        [JsonPropertyName("temp_c")] public decimal TempC { get; set; }
        [JsonPropertyName("condition")]
        public Condition Condition { get; set; }
    }

    public class Forecast
    {
        [JsonPropertyName("forecastday")]
        public List<ForecastDay> ForecastDay { get; set; }
    }

    public class ForecastDay
    {
        [JsonPropertyName("date")] public DateTime Date { get; set; }
        [JsonPropertyName("day")] public DayInfo Day { get; set; }
    }

    public class DayInfo
    {
        [JsonPropertyName("maxtemp_c")] public decimal MaxTempC { get; set; }
        [JsonPropertyName("mintemp_c")] public decimal MinTempC { get; set; }
        [JsonPropertyName("avgtemp_c")] public decimal AvgTempC { get; set; }
        [JsonPropertyName("condition")]
        public Condition Condition { get; set; }
    }

    public class Condition
    {
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("icon")] public string Icon { get; set; }
    }
}
