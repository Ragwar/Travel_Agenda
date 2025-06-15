using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TravelAgenda.Models;

namespace TravelAgenda.Services
{
	public interface IGooglePlacesService
	{
		Task<CityDetailsResult> GetCityDetailsAsync(string placeId);
		Task<List<PlaceResult>> GetTopLocationsAsync(string placeId, string locationType, int maxResults = 10);
		Task<List<PlaceResult>> GetHotelsAsync(string placeId, int maxResults = 20);
		Task<PlaceDetailsResult> GetPlaceDetailsAsync(string placeId);
		Task<List<PlaceResult>> SearchPlacesAsync(string query, string cityPlaceId = null);
		Task<string> GetPlacePhotoUrlAsync(string photoReference, int maxWidth = 400);
	}

	public class GooglePlacesService : IGooglePlacesService
	{
		private readonly HttpClient _httpClient;
		private readonly string _apiKey;
		private const string BaseUrl = "https://maps.googleapis.com/maps/api/place";

		public GooglePlacesService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
		{
			_httpClient = httpClientFactory.CreateClient();
			_apiKey = configuration["GoogleAPI:ApiKey"];
		}

		public async Task<CityDetailsResult> GetCityDetailsAsync(string placeId)
		{
			var url = $"{BaseUrl}/details/json?place_id={placeId}&fields=name,geometry,formatted_address,photos&key={_apiKey}";

			var response = await _httpClient.GetAsync(url);
			response.EnsureSuccessStatusCode();

			var json = await response.Content.ReadAsStringAsync();
			var result = JsonConvert.DeserializeObject<GooglePlaceDetailsResponse>(json);

			if (result?.Result == null)
				return null;

			return new CityDetailsResult
			{
				Name = result.Result.Name,
				PlaceId = placeId,
				FormattedAddress = result.Result.FormattedAddress,
				Location = result.Result.Geometry?.Location,
				Viewport = result.Result.Geometry?.Viewport,
				Photos = result.Result.Photos
			};
		}

		public async Task<List<PlaceResult>> GetTopLocationsAsync(string placeId, string locationType, int maxResults = 10)
		{
			// First get city details to get the viewport
			var cityDetails = await GetCityDetailsAsync(placeId);
			if (cityDetails == null)
				return new List<PlaceResult>();

			var places = new List<PlaceResult>();

			// Use nearby search with city location
			var nearbyUrl = $"{BaseUrl}/nearbysearch/json?" +
				$"location={cityDetails.Location.Lat},{cityDetails.Location.Lng}" +
				$"&radius=15000" + // 15km radius
				$"&type={locationType}" +
				$"&key={_apiKey}";

			var nearbyResponse = await _httpClient.GetAsync(nearbyUrl);
			if (nearbyResponse.IsSuccessStatusCode)
			{
				var nearbyJson = await nearbyResponse.Content.ReadAsStringAsync();
				var nearbyResult = JsonConvert.DeserializeObject<GooglePlacesSearchResponse>(nearbyJson);
				if (nearbyResult?.Results != null)
				{
					places.AddRange(nearbyResult.Results.Select(r => ConvertToPlaceResult(r, locationType)));
				}
			}

			// Also do text search for better results
			var searchQueries = GetSearchQueriesForType(locationType, cityDetails.Name);
			foreach (var query in searchQueries)
			{
				var textSearchUrl = $"{BaseUrl}/textsearch/json?" +
					$"query={Uri.EscapeDataString(query)}" +
					$"&location={cityDetails.Location.Lat},{cityDetails.Location.Lng}" +
					$"&radius=15000" +
					$"&key={_apiKey}";

				var textResponse = await _httpClient.GetAsync(textSearchUrl);
				if (textResponse.IsSuccessStatusCode)
				{
					var textJson = await textResponse.Content.ReadAsStringAsync();
					var textResult = JsonConvert.DeserializeObject<GooglePlacesSearchResponse>(textJson);
					if (textResult?.Results != null)
					{
						places.AddRange(textResult.Results.Select(r => ConvertToPlaceResult(r, locationType)));
					}
				}

				// Add delay to avoid rate limiting
				await Task.Delay(500);
			}

			// Remove duplicates and filter
			var uniquePlaces = places
				.GroupBy(p => p.PlaceId)
				.Select(g => g.First())
				.Where(p => p.Rating >= 3.0 && p.UserRatingsTotal >= 5)
				.Where(p => IsCorrectType(p, locationType))
				.OrderByDescending(p => p.UserRatingsTotal)
				.ThenByDescending(p => p.Rating)
				.Take(maxResults)
				.ToList();

			return uniquePlaces;
		}

		public async Task<List<PlaceResult>> GetHotelsAsync(string placeId, int maxResults = 20)
		{
			return await GetTopLocationsAsync(placeId, "lodging", maxResults);
		}

		public async Task<PlaceDetailsResult> GetPlaceDetailsAsync(string placeId)
		{
			var url = $"{BaseUrl}/details/json?" +
				$"place_id={placeId}" +
				$"&fields=name,rating,user_ratings_total,formatted_address,formatted_phone_number,website,opening_hours,reviews,editorial_summary,types,photos,geometry,price_level" +
				$"&key={_apiKey}";

			var response = await _httpClient.GetAsync(url);
			if (!response.IsSuccessStatusCode)
				return null;

			var json = await response.Content.ReadAsStringAsync();
			var result = JsonConvert.DeserializeObject<GooglePlaceDetailsResponse>(json);

			if (result?.Result == null)
				return null;

			return new PlaceDetailsResult
			{
				PlaceId = placeId,
				Name = result.Result.Name,
				FormattedAddress = result.Result.FormattedAddress,
				FormattedPhoneNumber = result.Result.FormattedPhoneNumber,
				Website = result.Result.Website,
				Rating = result.Result.Rating,
				UserRatingsTotal = result.Result.UserRatingsTotal,
				PriceLevel = result.Result.PriceLevel,
				Types = result.Result.Types,
				OpeningHours = result.Result.OpeningHours,
				Reviews = result.Result.Reviews?.Select(r => new ReviewResult
				{
					AuthorName = r.AuthorName,
					Rating = r.Rating,
					Text = r.Text,
					Time = r.Time
				}).ToList(),
				EditorialSummary = result.Result.EditorialSummary?.Overview,
				Photos = result.Result.Photos,
				Location = result.Result.Geometry?.Location
			};
		}

		public async Task<List<PlaceResult>> SearchPlacesAsync(string query, string cityPlaceId = null)
		{
			var url = $"{BaseUrl}/textsearch/json?query={Uri.EscapeDataString(query)}";

			if (!string.IsNullOrEmpty(cityPlaceId))
			{
				var cityDetails = await GetCityDetailsAsync(cityPlaceId);
				if (cityDetails != null)
				{
					url += $"&location={cityDetails.Location.Lat},{cityDetails.Location.Lng}&radius=15000";
				}
			}

			url += $"&key={_apiKey}";

			var response = await _httpClient.GetAsync(url);
			if (!response.IsSuccessStatusCode)
				return new List<PlaceResult>();

			var json = await response.Content.ReadAsStringAsync();
			var result = JsonConvert.DeserializeObject<GooglePlacesSearchResponse>(json);

			if (result?.Results == null)
				return new List<PlaceResult>();

			return result.Results
				.Select(r => ConvertToPlaceResult(r, "search"))
				.Where(p => p.Rating >= 3.0 && p.UserRatingsTotal >= 5)
				.OrderByDescending(p => p.UserRatingsTotal)
				.ThenByDescending(p => p.Rating)
				.Take(50)
				.ToList();
		}

		public async Task<string> GetPlacePhotoUrlAsync(string photoReference, int maxWidth = 400)
		{
			if (string.IsNullOrEmpty(photoReference))
				return null;

			return $"{BaseUrl}/photo?maxwidth={maxWidth}&photo_reference={photoReference}&key={_apiKey}";
		}

		private List<string> GetSearchQueriesForType(string locationType, string cityName)
		{
			var queries = new Dictionary<string, List<string>>
			{
				["restaurant"] = new List<string>
				{
					$"restaurants in {cityName}",
					$"best restaurants {cityName}",
					$"dining {cityName}"
				},
				["lodging"] = new List<string>
				{
					$"hotels in {cityName}",
					$"accommodation {cityName}",
					$"lodging {cityName}"
				},
				["park"] = new List<string>
				{
					$"parks in {cityName}",
					$"gardens {cityName}",
					$"recreation {cityName}"
				},
				["museum"] = new List<string>
				{
					$"museums in {cityName}",
					$"galleries {cityName}",
					$"culture {cityName}"
				},
				["shopping_mall"] = new List<string>
				{
					$"shopping in {cityName}",
					$"malls {cityName}",
					$"stores {cityName}"
				}
			};

			return queries.ContainsKey(locationType) ? queries[locationType] : new List<string> { $"{locationType} in {cityName}" };
		}

		private bool IsCorrectType(PlaceResult place, string targetType)
		{
			if (place.Types == null || !place.Types.Any())
				return false;

			var typeMapping = new Dictionary<string, List<string>>
			{
				["restaurant"] = new List<string> { "restaurant", "meal_takeaway", "meal_delivery", "food", "cafe", "bar" },
				["lodging"] = new List<string> { "lodging", "hotel", "resort", "hostel", "motel", "bed_and_breakfast", "guest_house" },
				["park"] = new List<string> { "park", "natural_feature", "campground", "rv_park" },
				["museum"] = new List<string> { "museum", "art_gallery", "library", "cultural_center" },
				["shopping_mall"] = new List<string> { "shopping_mall", "department_store", "store", "clothing_store", "electronics_store" }
			};

			if (!typeMapping.ContainsKey(targetType))
				return true;

			return place.Types.Any(t => typeMapping[targetType].Contains(t));
		}

		private PlaceResult ConvertToPlaceResult(GooglePlaceSearchResult googleResult, string locationType)
		{
			return new PlaceResult
			{
				PlaceId = googleResult.PlaceId,
				Name = googleResult.Name,
				FormattedAddress = googleResult.FormattedAddress,
				Vicinity = googleResult.Vicinity,
				Rating = googleResult.Rating,
				UserRatingsTotal = googleResult.UserRatingsTotal,
				PriceLevel = googleResult.PriceLevel,
				Types = googleResult.Types,
				Photos = googleResult.Photos,
				Location = googleResult.Geometry?.Location,
				LocationType = locationType
			};
		}
	}

	// Models for API responses
	public class GooglePlacesSearchResponse
	{
		[JsonProperty("results")]
		public List<GooglePlaceSearchResult> Results { get; set; }

		[JsonProperty("status")]
		public string Status { get; set; }

		[JsonProperty("next_page_token")]
		public string NextPageToken { get; set; }
	}

	public class GooglePlaceSearchResult
	{
		[JsonProperty("place_id")]
		public string PlaceId { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("formatted_address")]
		public string FormattedAddress { get; set; }

		[JsonProperty("vicinity")]
		public string Vicinity { get; set; }

		[JsonProperty("rating")]
		public double Rating { get; set; }

		[JsonProperty("user_ratings_total")]
		public int UserRatingsTotal { get; set; }

		[JsonProperty("price_level")]
		public int? PriceLevel { get; set; }

		[JsonProperty("types")]
		public List<string> Types { get; set; }

		[JsonProperty("photos")]
		public List<GooglePhoto> Photos { get; set; }

		[JsonProperty("geometry")]
		public GoogleGeometry Geometry { get; set; }
	}

	public class GooglePlaceDetailsResponse
	{
		[JsonProperty("result")]
		public GooglePlaceDetailsResult Result { get; set; }

		[JsonProperty("status")]
		public string Status { get; set; }
	}

	public class GooglePlaceDetailsResult
	{
		[JsonProperty("place_id")]
		public string PlaceId { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("formatted_address")]
		public string FormattedAddress { get; set; }

		[JsonProperty("formatted_phone_number")]
		public string FormattedPhoneNumber { get; set; }

		[JsonProperty("website")]
		public string Website { get; set; }

		[JsonProperty("rating")]
		public double Rating { get; set; }

		[JsonProperty("user_ratings_total")]
		public int UserRatingsTotal { get; set; }

		[JsonProperty("price_level")]
		public int? PriceLevel { get; set; }

		[JsonProperty("types")]
		public List<string> Types { get; set; }

		[JsonProperty("opening_hours")]
		public GoogleOpeningHours OpeningHours { get; set; }

		[JsonProperty("reviews")]
		public List<GoogleReview> Reviews { get; set; }

		[JsonProperty("editorial_summary")]
		public GoogleEditorialSummary EditorialSummary { get; set; }

		[JsonProperty("photos")]
		public List<GooglePhoto> Photos { get; set; }

		[JsonProperty("geometry")]
		public GoogleGeometry Geometry { get; set; }
	}

	public class GoogleGeometry
	{
		[JsonProperty("location")]
		public GoogleLocation Location { get; set; }

		[JsonProperty("viewport")]
		public GoogleViewport Viewport { get; set; }
	}

	public class GoogleLocation
	{
		[JsonProperty("lat")]
		public double Lat { get; set; }

		[JsonProperty("lng")]
		public double Lng { get; set; }
	}

	public class GoogleViewport
	{
		[JsonProperty("northeast")]
		public GoogleLocation Northeast { get; set; }

		[JsonProperty("southwest")]
		public GoogleLocation Southwest { get; set; }
	}

	public class GooglePhoto
	{
		[JsonProperty("photo_reference")]
		public string PhotoReference { get; set; }

		[JsonProperty("height")]
		public int Height { get; set; }

		[JsonProperty("width")]
		public int Width { get; set; }
	}

	public class GoogleOpeningHours
	{
		[JsonProperty("weekday_text")]
		public List<string> WeekdayText { get; set; }

		[JsonProperty("open_now")]
		public bool? OpenNow { get; set; }
	}

	public class GoogleReview
	{
		[JsonProperty("author_name")]
		public string AuthorName { get; set; }

		[JsonProperty("rating")]
		public int Rating { get; set; }

		[JsonProperty("text")]
		public string Text { get; set; }

		[JsonProperty("time")]
		public long Time { get; set; }
	}

	public class GoogleEditorialSummary
	{
		[JsonProperty("overview")]
		public string Overview { get; set; }
	}

	// Result models for our service
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
		public string PhotoUrl { get; set; } // Computed property for first photo
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
}