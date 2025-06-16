
using TravelAgenda.Models;
using Newtonsoft.Json;

namespace TravelAgenda.Services.Interfaces
{
	public interface IGooglePlacesService
	{
		// City-related methods
		Task<CityDetailsResult> GetCityDetailsAsync(string placeId);
		Task<List<PlaceResult>> GetTopLocationsByCityAsync(string cityPlaceId, string cityName, string locationType, int maxResults = 10);

		// Hotel/residence-related methods
		Task<List<PlaceResult>> GetHotelsInCityAsync(string cityPlaceId, string cityName, int maxResults = 20);

		// Activity/location search methods
		Task<List<PlaceResult>> SearchPlacesAsync(string query, string cityPlaceId = null, string cityName = null);
		Task<List<PlaceResult>> GetLocationsByTypeAsync(string cityPlaceId, string cityName, string locationType, int maxResults = 50);

		// Place details
		Task<PlaceDetailsResult> GetPlaceDetailsAsync(string placeId);

		// Photo utilities
		Task<string> GetPlacePhotoUrlAsync(string photoReference, int maxWidth = 400);
	}
	public class CityDetailsResult
	{
		public string Name { get; set; }
		public string PlaceId { get; set; }
		public string FormattedAddress { get; set; }
		public GoogleLocation Location { get; set; }
		public GoogleViewport Viewport { get; set; }
		public List<GooglePhoto> Photos { get; set; }
	}

	public class PlaceResult
	{
		public string PlaceId { get; set; }
		public string Name { get; set; }
		public string FormattedAddress { get; set; }
		public string Vicinity { get; set; }
		public double Rating { get; set; }
		public int UserRatingsTotal { get; set; }
		public int? PriceLevel { get; set; }
		public List<string> Types { get; set; }
		public List<GooglePhoto> Photos { get; set; }
		public GoogleLocation Location { get; set; }
		public string LocationType { get; set; }
		public string PhotoUrl { get; set; }
		public GoogleOpeningHours OpeningHours { get; set; }
	}

	public class PlaceDetailsResult
	{
		public string PlaceId { get; set; }
		public string Name { get; set; }
		public string FormattedAddress { get; set; }
		public string FormattedPhoneNumber { get; set; }
		public string Website { get; set; }
		public double Rating { get; set; }
		public int UserRatingsTotal { get; set; }
		public int? PriceLevel { get; set; }
		public List<string> Types { get; set; }
		public GoogleOpeningHours OpeningHours { get; set; }
		public List<ReviewResult> Reviews { get; set; }
		public string EditorialSummary { get; set; }
		public List<GooglePhoto> Photos { get; set; }
		public GoogleLocation Location { get; set; }
	}

	public class ReviewResult
	{
		public string AuthorName { get; set; }
		public int Rating { get; set; }
		public string Text { get; set; }
		public long Time { get; set; }
	}

	// Simple result models for your service responses
	public class GoogleLocation
	{
		public double Lat { get; set; }
		public double Lng { get; set; }
	}

	public class GoogleViewport
	{
		public GoogleLocation Northeast { get; set; }
		public GoogleLocation Southwest { get; set; }
	}

	public class GooglePhoto
	{
		public string PhotoReference { get; set; }
		public int Height { get; set; }
		public int Width { get; set; }
		public string PhotoUrl { get; set; }
	}

	// FULL Google API structure for opening hours (with Periods, etc.)
	public class GoogleOpeningHours
	{
		[JsonProperty("open_now")]
		public bool? OpenNow { get; set; }

		[JsonProperty("periods")]
		public List<GooglePeriod> Periods { get; set; }

		[JsonProperty("weekday_text")]
		public List<string> WeekdayText { get; set; }
	}

	public class GooglePeriod
	{
		[JsonProperty("close")]
		public GoogleTimeOfDay Close { get; set; }

		[JsonProperty("open")]
		public GoogleTimeOfDay Open { get; set; }
	}

	public class GoogleTimeOfDay
	{
		[JsonProperty("day")]
		public int Day { get; set; }

		[JsonProperty("time")]
		public string Time { get; set; }
	}
}