using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TravelAgenda.Services.Interfaces;

namespace TravelAgenda.Services
{
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

			// Add photo URLs to photos
			if (result.Result.Photos != null)
			{
				foreach (var photo in result.Result.Photos)
				{
					photo.PhotoUrl = GetPhotoUrl(photo.PhotoReference);
				}
			}

			return new CityDetailsResult
			{
				Name = result.Result.Name,
				PlaceId = placeId,
				FormattedAddress = result.Result.FormattedAddress,
				Location = MapLocation(result.Result.Geometry?.Location),
				Viewport = MapViewport(result.Result.Geometry?.Viewport),
				Photos = MapPhotos(result.Result.Photos)
			};
		}

		public async Task<List<PlaceResult>> GetTopLocationsByCityAsync(string cityPlaceId, string cityName, string locationType, int maxResults = 10)
		{
			// First get city details to get the viewport bounds
			var cityDetails = await GetCityDetailsAsync(cityPlaceId);
			if (cityDetails == null)
				return new List<PlaceResult>();

			var places = new List<PlaceResult>();

			// Calculate expanded search radius based on city bounds
			var searchRadius = CalculateSearchRadius(cityDetails.Viewport);

			// Use nearby search with city location
			var nearbyUrl = $"{BaseUrl}/nearbysearch/json?" +
				$"location={cityDetails.Location.Lat},{cityDetails.Location.Lng}" +
				$"&radius={searchRadius}" +
				$"&type={locationType}" +
				$"&key={_apiKey}";

			var nearbyResponse = await _httpClient.GetAsync(nearbyUrl);
			if (nearbyResponse.IsSuccessStatusCode)
			{
				var nearbyJson = await nearbyResponse.Content.ReadAsStringAsync();
				var nearbyResult = JsonConvert.DeserializeObject<GooglePlacesSearchResponse>(nearbyJson);
				if (nearbyResult?.Results != null)
				{
					places.AddRange(nearbyResult.Results
						.Where(r => IsWithinCityBounds(r, cityDetails.Viewport))
						.Select(r => ConvertToPlaceResult(r, locationType)));
				}
			}

			// Also do text search for better results
			var searchQueries = GetSearchQueriesForType(locationType, cityName);
			foreach (var query in searchQueries)
			{
				var textSearchUrl = $"{BaseUrl}/textsearch/json?" +
					$"query={Uri.EscapeDataString(query)}" +
					$"&location={cityDetails.Location.Lat},{cityDetails.Location.Lng}" +
					$"&radius={searchRadius}" +
					$"&key={_apiKey}";

				var textResponse = await _httpClient.GetAsync(textSearchUrl);
				if (textResponse.IsSuccessStatusCode)
				{
					var textJson = await textResponse.Content.ReadAsStringAsync();
					var textResult = JsonConvert.DeserializeObject<GooglePlacesSearchResponse>(textJson);
					if (textResult?.Results != null)
					{
						places.AddRange(textResult.Results
							.Where(r => IsWithinCityBounds(r, cityDetails.Viewport))
							.Select(r => ConvertToPlaceResult(r, locationType)));
					}
				}

				// Add delay to avoid rate limiting
				await Task.Delay(500);
			}

			// Remove duplicates and filter by rating, then sort by most rated
			var uniquePlaces = places
				.GroupBy(p => p.PlaceId)
				.Select(g => g.First())
				.Where(p => p.Rating >= 3.0 && p.UserRatingsTotal >= 5)
				.Where(p => IsCorrectType(p, locationType))
				.OrderByDescending(p => p.UserRatingsTotal) // Most rated first
				.ThenByDescending(p => p.Rating) // Then by highest rating
				.Take(maxResults)
				.ToList();

			return uniquePlaces;
		}

		public async Task<List<PlaceResult>> GetHotelsInCityAsync(string cityPlaceId, string cityName, int maxResults = 20)
		{
			return await GetTopLocationsByCityAsync(cityPlaceId, cityName, "lodging", maxResults);
		}

		public async Task<List<PlaceResult>> GetLocationsByTypeAsync(string cityPlaceId, string cityName, string locationType, int maxResults = 50)
		{
			return await GetTopLocationsByCityAsync(cityPlaceId, cityName, locationType, maxResults);
		}

		public async Task<List<PlaceResult>> SearchPlacesAsync(string query, string cityPlaceId = null, string cityName = null)
		{
			var url = $"{BaseUrl}/textsearch/json?query={Uri.EscapeDataString(query)}";

			GoogleViewport cityViewport = null;

			if (!string.IsNullOrEmpty(cityPlaceId))
			{
				var cityDetails = await GetCityDetailsAsync(cityPlaceId);
				if (cityDetails != null)
				{
					var searchRadius = CalculateSearchRadius(cityDetails.Viewport);
					url += $"&location={cityDetails.Location.Lat},{cityDetails.Location.Lng}&radius={searchRadius}";
					cityViewport = cityDetails.Viewport;
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

			var filteredResults = result.Results.AsEnumerable();

			// Apply city bounds check if we have city viewport
			if (cityViewport != null)
			{
				filteredResults = filteredResults.Where(r => IsWithinCityBounds(r, cityViewport));
			}

			return filteredResults
				.Select(r => ConvertToPlaceResult(r, "search"))
				.Where(p => p.Rating >= 3.0 && p.UserRatingsTotal >= 5)
				.OrderByDescending(p => p.UserRatingsTotal) // Most rated first
				.ThenByDescending(p => p.Rating) // Then by highest rating
				.Take(50)
				.ToList();
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

			// Add photo URLs to photos
			if (result.Result.Photos != null)
			{
				foreach (var photo in result.Result.Photos)
				{
					photo.PhotoUrl = GetPhotoUrl(photo.PhotoReference);
				}
			}

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
				OpeningHours = MapOpeningHours(result.Result.OpeningHours),
				Reviews = result.Result.Reviews?.Select(r => new ReviewResult
				{
					AuthorName = r.AuthorName,
					Rating = r.Rating,
					Text = r.Text,
					Time = r.Time
				}).ToList(),
				EditorialSummary = result.Result.EditorialSummary?.Overview,
				Photos = MapPhotos(result.Result.Photos),
				Location = MapLocation(result.Result.Geometry?.Location)
			};
		}

		public async Task<string> GetPlacePhotoUrlAsync(string photoReference, int maxWidth = 400)
		{
			if (string.IsNullOrEmpty(photoReference))
				return null;

			return GetPhotoUrl(photoReference, maxWidth);
		}

		// SIMPLE CITY BOUNDS CHECKING METHODS

		/// <summary>
		/// Calculate appropriate search radius based on city viewport bounds
		/// </summary>
		private int CalculateSearchRadius(GoogleViewport viewport)
		{
			if (viewport?.Northeast == null || viewport?.Southwest == null)
				return 15000; // Default 15km

			// Calculate the distance between northeast and southwest corners
			var latDiff = viewport.Northeast.Lat - viewport.Southwest.Lat;
			var lngDiff = viewport.Northeast.Lng - viewport.Southwest.Lng;

			// Calculate approximate diagonal distance in kilometers
			// Using Haversine formula approximation for small distances
			var avgLat = (viewport.Northeast.Lat + viewport.Southwest.Lat) / 2;
			var latDistance = latDiff * 111; // 1 degree lat ≈ 111km
			var lngDistance = lngDiff * 111 * Math.Cos(avgLat * Math.PI / 180); // Adjust for longitude

			var diagonalDistance = Math.Sqrt(latDistance * latDistance + lngDistance * lngDistance);

			// Use the diagonal distance as radius, with some padding (multiply by 0.7 to stay within bounds)
			// Convert to meters and add some buffer
			var radiusKm = Math.Max(5, diagonalDistance * 0.7); // Minimum 5km
			var radiusMeters = (int)(radiusKm * 1000);

			// Cap at reasonable maximum
			return Math.Min(radiusMeters, 30000); // Maximum 30km
		}

		/// <summary>
		/// Simple check if a place is within the city viewport bounds
		/// </summary>
		private bool IsWithinCityBounds(GooglePlaceSearchResult place, GoogleViewport cityViewport)
		{
			// If no viewport or location, assume it's valid
			if (cityViewport?.Northeast == null || cityViewport?.Southwest == null ||
				place.Geometry?.Location == null)
				return true;

			var location = place.Geometry.Location;

			// Simple bounding box check
			return location.Lat <= cityViewport.Northeast.Lat &&
				   location.Lat >= cityViewport.Southwest.Lat &&
				   location.Lng <= cityViewport.Northeast.Lng &&
				   location.Lng >= cityViewport.Southwest.Lng;
		}

		private string GetPhotoUrl(string photoReference, int maxWidth = 400)
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
					$"dining {cityName}",
					$"food {cityName}"
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
			var result = new PlaceResult
			{
				PlaceId = googleResult.PlaceId,
				Name = googleResult.Name,
				FormattedAddress = googleResult.FormattedAddress,
				Vicinity = googleResult.Vicinity,
				Rating = googleResult.Rating,
				UserRatingsTotal = googleResult.UserRatingsTotal,
				PriceLevel = googleResult.PriceLevel,
				Types = googleResult.Types,
				Photos = MapPhotos(googleResult.Photos),
				Location = MapLocation(googleResult.Geometry?.Location),
				LocationType = locationType
			};

			// Add photo URL to the first photo if available
			if (result.Photos != null && result.Photos.Count > 0)
			{
				var firstPhoto = result.Photos[0];
				firstPhoto.PhotoUrl = GetPhotoUrl(firstPhoto.PhotoReference, 100);
				result.PhotoUrl = firstPhoto.PhotoUrl;
			}

			return result;
		}

		// MAPPING METHODS
		private GoogleOpeningHours MapOpeningHours(GoogleOpeningHoursResponse source)
		{
			if (source == null) return null;

			return new GoogleOpeningHours
			{
				OpenNow = source.OpenNow,
				WeekdayText = source.WeekdayText,
				Periods = source.Periods?.Select(p => new GooglePeriod
				{
					Open = p.Open != null ? new GoogleTimeOfDay
					{
						Day = p.Open.Day,
						Time = p.Open.Time
					} : null,
					Close = p.Close != null ? new GoogleTimeOfDay
					{
						Day = p.Close.Day,
						Time = p.Close.Time
					} : null
				}).ToList()
			};
		}

		private List<GooglePhoto> MapPhotos(List<GooglePhotoResponse> source)
		{
			if (source == null) return null;

			return source.Select(p => new GooglePhoto
			{
				PhotoReference = p.PhotoReference,
				Height = p.Height,
				Width = p.Width,
				PhotoUrl = p.PhotoUrl
			}).ToList();
		}

		private GoogleLocation MapLocation(GoogleLocationResponse source)
		{
			if (source == null) return null;

			return new GoogleLocation
			{
				Lat = source.Lat,
				Lng = source.Lng
			};
		}

		private GoogleViewport MapViewport(GoogleViewportResponse source)
		{
			if (source == null) return null;

			return new GoogleViewport
			{
				Northeast = MapLocation(source.Northeast),
				Southwest = MapLocation(source.Southwest)
			};
		}
	}

	// INTERNAL GOOGLE API RESPONSE MODELS (ONLY ONE SET - NO DUPLICATES)
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
		public List<GooglePhotoResponse> Photos { get; set; }

		[JsonProperty("geometry")]
		public GoogleGeometryResponse Geometry { get; set; }
	}

	public class GooglePlaceDetailsResponse
	{
		[JsonProperty("result")]
		public GooglePlaceDetailsApiResult Result { get; set; }

		[JsonProperty("status")]
		public string Status { get; set; }
	}

	public class GooglePlaceDetailsApiResult
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
		public GoogleOpeningHoursResponse OpeningHours { get; set; }

		[JsonProperty("reviews")]
		public List<GoogleReviewResponse> Reviews { get; set; }

		[JsonProperty("editorial_summary")]
		public GoogleEditorialSummaryResponse EditorialSummary { get; set; }

		[JsonProperty("photos")]
		public List<GooglePhotoResponse> Photos { get; set; }

		[JsonProperty("geometry")]
		public GoogleGeometryResponse Geometry { get; set; }
	}

	public class GoogleGeometryResponse
	{
		[JsonProperty("location")]
		public GoogleLocationResponse Location { get; set; }

		[JsonProperty("viewport")]
		public GoogleViewportResponse Viewport { get; set; }
	}

	public class GoogleLocationResponse
	{
		[JsonProperty("lat")]
		public double Lat { get; set; }

		[JsonProperty("lng")]
		public double Lng { get; set; }
	}

	public class GoogleViewportResponse
	{
		[JsonProperty("northeast")]
		public GoogleLocationResponse Northeast { get; set; }

		[JsonProperty("southwest")]
		public GoogleLocationResponse Southwest { get; set; }
	}

	public class GoogleReviewResponse
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

	public class GoogleEditorialSummaryResponse
	{
		[JsonProperty("overview")]
		public string Overview { get; set; }
	}

	public class GooglePhotoResponse
	{
		[JsonProperty("photo_reference")]
		public string PhotoReference { get; set; }

		[JsonProperty("height")]
		public int Height { get; set; }

		[JsonProperty("width")]
		public int Width { get; set; }

		public string PhotoUrl { get; set; }
	}

	public class GoogleOpeningHoursResponse
	{
		[JsonProperty("open_now")]
		public bool? OpenNow { get; set; }

		[JsonProperty("periods")]
		public List<GooglePeriodResponse> Periods { get; set; }

		[JsonProperty("weekday_text")]
		public List<string> WeekdayText { get; set; }
	}

	public class GooglePeriodResponse
	{
		[JsonProperty("close")]
		public GoogleTimeOfDayResponse Close { get; set; }

		[JsonProperty("open")]
		public GoogleTimeOfDayResponse Open { get; set; }
	}

	public class GoogleTimeOfDayResponse
	{
		[JsonProperty("day")]
		public int Day { get; set; }

		[JsonProperty("time")]
		public string Time { get; set; }
	}
}