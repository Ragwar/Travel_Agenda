using System.Threading.Tasks;
using TravelAgenda.Models;
using TravelAgenda.Services.Models;

namespace TravelAgenda.Services.Interfaces
{
    public interface IWeatherService
    {
        /// <summary>
        /// Gets the weather forecast for the given location (name or lat,lng) for the specified number of days.
        /// </summary>
        Task<WeatherForecastResponse> GetForecastAsync(string locationQuery, int days);
    }
}
