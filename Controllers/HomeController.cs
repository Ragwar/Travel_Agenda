using GoogleApi.Entities.Search.Video.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using System.Diagnostics;
using TravelAgenda.Data;
using TravelAgenda.Models;
using TravelAgenda.Services;
using TravelAgenda.Services.Interfaces;

namespace TravelAgenda.Controllers
{
	public class HomeController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IScheduleService _scheduleService;
		private readonly IScheduleActivityService _ScheduleActivityProductService;
		private readonly IUserInfoService _userInfoService;
		private readonly IUserService _userService;
		private readonly IGooglePlacesService _googlePlacesService; // New service
		private readonly string _googleApiKey;
		private readonly ChatClient _chatClient;
		private readonly IGoogleCalendarService _googleCalendarService;

		public HomeController(ApplicationDbContext context,
			IScheduleService scheduleService,
			IScheduleActivityService ScheduleActivityProductService,
			IUserInfoService userInfoService,
			IUserService userService,
			IConfiguration configuration,
			IGoogleCalendarService googleCalendarService,
			IGooglePlacesService googlePlacesService) // Inject new service
		{
			_scheduleService = scheduleService;
			_ScheduleActivityProductService = ScheduleActivityProductService;
			_userInfoService = userInfoService;
			_userService = userService;
			_googlePlacesService = googlePlacesService;
			_googleApiKey = configuration["GoogleAPI:ApiKey"];
			_context = context;
			_googleCalendarService = googleCalendarService;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult TimePeriod(int? scheduleId)
		{
			ViewData["GoogleApiKey"] = _googleApiKey;

			if (scheduleId.HasValue)
			{
				var schedule = _scheduleService.GetScheduleById(scheduleId.Value);
				if (schedule == null)
				{
					return NotFound("Schedule not found.");
				}
				ViewBag.Schedule = schedule;
				ViewBag.ScheduleId = scheduleId.Value;
				ViewBag.IsExistingSchedule = true;
			}
			else
			{
				ViewBag.IsExistingSchedule = false;
			}

			return View();
		}

		public IActionResult City(int? scheduleId)
		{
			ViewData["GoogleApiKey"] = _googleApiKey;
			if (scheduleId.HasValue)
			{
				ViewBag.ScheduleId = scheduleId.Value;
				var schedule = _scheduleService.GetScheduleById(scheduleId.Value);
				if (schedule == null)
				{
					return NotFound("Schedule not found.");
				}
				ViewBag.Schedule = schedule;
			}
			return View();
		}

		public IActionResult SchedulesList(string id)
		{
			ViewData["GoogleApiKey"] = _googleApiKey;
			ViewBag.Schedules = _scheduleService.GetSchedulesByUserId(id);
			return View();
		}

		public IActionResult LocationsAndActivities(int scheduleId)
		{
			var schedule = _scheduleService.GetScheduleById(scheduleId);
			if (schedule == null) return NotFound();

			ViewBag.Schedule = schedule;
			ViewData["CityName"] = schedule.CityName;
			ViewData["PlaceId"] = schedule.PlaceId;
			ViewData["GoogleApiKey"] = _googleApiKey;

			var activities = _ScheduleActivityProductService
								 .GetScheduleActivityByScheduleId(scheduleId);
			return View(activities);
		}

		public IActionResult Residence(int scheduleId)
		{
			var schedule = _scheduleService.GetScheduleById(scheduleId);
			if (schedule == null) return NotFound();

			ViewBag.Schedule = schedule;
			ViewData["CityName"] = schedule.CityName;
			ViewData["PlaceId"] = schedule.PlaceId;
			ViewData["GoogleApiKey"] = _googleApiKey;

			return View();
		}

		public IActionResult ViewSchedule(int id)
		{
			var schedule = _scheduleService.GetScheduleById(id);
			var activities = _ScheduleActivityProductService.GetScheduleActivityByScheduleId(schedule.ScheduleId);
			ViewBag.Schedule = schedule;
			ViewBag.Activities = activities;

			ViewData["GoogleApiKey"] = _googleApiKey;
			return View();
		}

		public IActionResult ViewDay(int scheduleId, string date)
		{
			var schedule = _scheduleService.GetScheduleById(scheduleId);
			if (schedule == null) return NotFound();

			if (!DateTime.TryParse(date, out var selectedDate))
			{
				return BadRequest("Invalid date format");
			}

			var allActivities = _ScheduleActivityProductService.GetScheduleActivityByScheduleId(scheduleId);
			var dayActivities = allActivities
				.Where(a => a.StartDate.HasValue && a.StartDate.Value.Date == selectedDate.Date)
				.OrderBy(a => a.StartHour + ((a.StartMinute ?? 0) / 60.0))
				.ToList();

			// Add logging to debug
			Console.WriteLine($"ViewDay - Schedule: {schedule.CityName}, Date: {selectedDate:yyyy-MM-dd}");
			Console.WriteLine($"Total activities in schedule: {allActivities?.Count ?? 0}");
			Console.WriteLine($"Activities for selected day: {dayActivities.Count}");

			foreach (var activity in dayActivities)
			{
				Console.WriteLine($"Activity: {activity.Name}, PlaceId: {activity.PlaceId ?? "NULL"}, Type: {activity.Type}");
			}

			ViewBag.Schedule = schedule;
			ViewBag.DayActivities = dayActivities;
			ViewBag.SelectedDate = selectedDate;
			ViewData["GoogleApiKey"] = _googleApiKey;

			return View();
		}

		// ===== NEW API ENDPOINTS FOR PLACES SERVICE =====

		[HttpGet]
		public async Task<IActionResult> GetCityDetails(string placeId)
		{
			try
			{
				var cityDetails = await _googlePlacesService.GetCityDetailsAsync(placeId);
				return Ok(cityDetails);
			}
			catch (Exception ex)
			{
				return BadRequest($"Error getting city details: {ex.Message}");
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetTopLocationsByCity(string cityPlaceId, string cityName, string locationType, int maxResults = 10)
		{
			try
			{
				var locations = await _googlePlacesService.GetTopLocationsByCityAsync(cityPlaceId, cityName, locationType, maxResults);
				return Ok(locations);
			}
			catch (Exception ex)
			{
				return BadRequest($"Error getting top locations: {ex.Message}");
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetHotelsInCity(string cityPlaceId, string cityName, int maxResults = 20)
		{
			try
			{
				var hotels = await _googlePlacesService.GetHotelsInCityAsync(cityPlaceId, cityName, maxResults);
				return Ok(hotels);
			}
			catch (Exception ex)
			{
				return BadRequest($"Error getting hotels: {ex.Message}");
			}
		}

		[HttpGet]
		public async Task<IActionResult> SearchPlaces(string query, string cityPlaceId = null, string cityName = null)
		{
			try
			{
				Console.WriteLine($"SearchPlaces called with query: {query}, cityPlaceId: {cityPlaceId}, cityName: {cityName}");

				if (string.IsNullOrEmpty(query))
				{
					return BadRequest("Query is required");
				}

				var places = await _googlePlacesService.SearchPlacesAsync(query, cityPlaceId, cityName);

				Console.WriteLine($"Found {places?.Count ?? 0} places for query: {query}");

				if (places == null || places.Count == 0)
				{
					return Ok(new List<object>()); // Return empty array instead of null
				}

				// Transform to ensure consistent JSON structure
				var response = places.Select(p => new
				{
					placeId = p.PlaceId,
					name = p.Name,
					formattedAddress = p.FormattedAddress,
					vicinity = p.Vicinity,
					rating = p.Rating,
					userRatingsTotal = p.UserRatingsTotal,
					priceLevel = p.PriceLevel,
					types = p.Types,
					photos = p.Photos?.Select(ph => new
					{
						photoReference = ph.PhotoReference,
						height = ph.Height,
						width = ph.Width,
						photoUrl = ph.PhotoUrl
					}).ToList(),
					location = p.Location != null ? new
					{
						lat = p.Location.Lat,
						lng = p.Location.Lng
					} : null,
					locationType = p.LocationType,
					photoUrl = p.PhotoUrl
				}).ToList();

				return Json(response);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in SearchPlaces: {ex.Message}");
				Console.WriteLine($"Stack trace: {ex.StackTrace}");
				return StatusCode(500, $"Error searching places: {ex.Message}");
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetLocationsByType(string cityPlaceId, string cityName, string locationType, int maxResults = 50)
		{
			try
			{
				var locations = await _googlePlacesService.GetLocationsByTypeAsync(cityPlaceId, cityName, locationType, maxResults);
				return Ok(locations);
			}
			catch (Exception ex)
			{
				return BadRequest($"Error getting locations by type: {ex.Message}");
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetPlaceDetails(string placeId)
		{
			try
			{
				// Add logging
				Console.WriteLine($"GetPlaceDetails called with placeId: {placeId}");

				if (string.IsNullOrEmpty(placeId))
				{
					Console.WriteLine("PlaceId is null or empty");
					return BadRequest("PlaceId is required");
				}

				var details = await _googlePlacesService.GetPlaceDetailsAsync(placeId);

				if (details == null)
				{
					Console.WriteLine($"No details found for placeId: {placeId}");
					return NotFound($"No details found for place: {placeId}");
				}

				// Log what we got back
				Console.WriteLine($"Retrieved details for {details.Name}: {details.OpeningHours?.WeekdayText?.Count ?? 0} opening hours entries");

				// Ensure we return a proper JSON response
				var response = new
				{
					placeId = details.PlaceId,
					name = details.Name,
					formattedAddress = details.FormattedAddress,
					formattedPhoneNumber = details.FormattedPhoneNumber,
					website = details.Website,
					rating = details.Rating,
					userRatingsTotal = details.UserRatingsTotal,
					priceLevel = details.PriceLevel,
					types = details.Types,
					openingHours = details.OpeningHours != null ? new
					{
						openNow = details.OpeningHours.OpenNow,
						weekdayText = details.OpeningHours.WeekdayText,
						periods = details.OpeningHours.Periods
					} : null,
					reviews = details.Reviews?.Select(r => new
					{
						authorName = r.AuthorName,
						rating = r.Rating,
						text = r.Text,
						time = r.Time
					}).ToList(),
					editorialSummary = details.EditorialSummary,
					photos = details.Photos?.Select(p => new
					{
						photoReference = p.PhotoReference,
						height = p.Height,
						width = p.Width,
						photoUrl = p.PhotoUrl
					}).ToList(),
					location = details.Location != null ? new
					{
						lat = details.Location.Lat,
						lng = details.Location.Lng
					} : null
				};

				return Json(response);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in GetPlaceDetails: {ex.Message}");
				Console.WriteLine($"Stack trace: {ex.StackTrace}");
				return StatusCode(500, $"Error getting place details: {ex.Message}");
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetPlacePhotoUrl(string photoReference, int maxWidth = 400)
		{
			try
			{
				var photoUrl = await _googlePlacesService.GetPlacePhotoUrlAsync(photoReference, maxWidth);
				return Ok(new { photoUrl });
			}
			catch (Exception ex)
			{
				return BadRequest($"Error getting photo URL: {ex.Message}");
			}
		}

		// ===== EXISTING METHODS (unchanged) =====

		[HttpPost]
		public async Task<IActionResult> CreateNewSchedule()
		{
			var user = _userService.GetUserByName(User.Identity.Name);
			if (user == null)
			{
				return NotFound("User not found.");
			}
			var userId = user.Id;

			if (string.IsNullOrEmpty(userId))
			{
				return Unauthorized("User not logged in.");
			}

			var schedule = new Schedule
			{
				UserId = userId,
				StartDate = default(DateTime),
				EndDate = default(DateTime),
				NrDays = 0,
				StartDay = 0,
				EndDay = 0,
				StartMonth = 0,
				EndMonth = 0
			};

			_scheduleService.CreateSchedule(schedule);

			return Ok(new { scheduleId = schedule.ScheduleId });
		}

		public class UpdateDateRangeViewModel
		{
			public string StartDate { get; set; }
			public string EndDate { get; set; }
			public int ScheduleId { get; set; }
		}

		[HttpPost]
		public async Task<IActionResult> SaveDates([FromBody] UpdateDateRangeViewModel dateRange)
		{
			if (dateRange == null || string.IsNullOrEmpty(dateRange.StartDate) ||
					string.IsNullOrEmpty(dateRange.EndDate))
			{
				return BadRequest(new { error = "Invalid date range." });
			}

			if (!DateTime.TryParse(dateRange.StartDate, out var startDate) ||
				!DateTime.TryParse(dateRange.EndDate, out var endDate))
			{
				return BadRequest(new { error = "Invalid date format." });
			}

			var user = _userService.GetUserByName(User.Identity.Name);
			if (user == null)
			{
				return NotFound(new { error = "User not found." });
			}

			Schedule schedule;

			if (dateRange.ScheduleId > 0)
			{
				schedule = _scheduleService.GetScheduleById(dateRange.ScheduleId);
				if (schedule == null)
				{
					return NotFound("Schedule not found.");
				}

				if (schedule.UserId != user.Id)
				{
					return Unauthorized("You don't have permission to modify this schedule.");
				}
			}
			else
			{
				schedule = new Schedule
				{
					UserId = user.Id,
					StartDate = null,
					EndDate = null,
					NrDays = null,
					StartDay = null,
					EndDay = null,
					StartMonth = null,
					EndMonth = null
				};

				_scheduleService.CreateSchedule(schedule);
			}

			var nrDays = (endDate - startDate).Days + 1;

			schedule.StartDate = startDate;
			schedule.EndDate = endDate;
			schedule.NrDays = nrDays;
			schedule.StartDay = startDate.Day;
			schedule.EndDay = endDate.Day;
			schedule.StartMonth = startDate.Month;
			schedule.EndMonth = endDate.Month;

			_scheduleService.UpdateSchedule(schedule);

			return Ok(new { scheduleId = schedule.ScheduleId });
		}

		[HttpPost]
		public async Task<IActionResult> SaveCity([FromBody] CityViewModel city)
		{
			if (city == null || string.IsNullOrEmpty(city.Name) || string.IsNullOrEmpty(city.PlaceId))
			{
				return BadRequest("Invalid city data.");
			}

			var user = _userService.GetUserByName(User.Identity.Name);
			if (user == null)
			{
				return NotFound("User not found.");
			}
			var userId = user.Id;

			if (string.IsNullOrEmpty(userId))
			{
				return Unauthorized("User not logged in.");
			}

			try
			{
				var schedule = _scheduleService.GetScheduleById(city.ScheduleId);
				if (schedule == null)
				{
					return NotFound("Schedule not found.");
				}

				bool cityChanged = !string.IsNullOrEmpty(schedule.CityName) &&
								  !string.Equals(schedule.CityName, city.Name, StringComparison.OrdinalIgnoreCase);

				if (cityChanged)
				{
					schedule.HotelId = null;
					schedule.HotelName = null;

					var existingActivities = _ScheduleActivityProductService.GetScheduleActivityByScheduleId(city.ScheduleId);

					if (existingActivities != null && existingActivities.Count > 0)
					{
						foreach (var activity in existingActivities)
						{
							_ScheduleActivityProductService.DeleteScheduleActivity(activity);
						}
					}
				}

				schedule.CityName = city.Name;
				schedule.PlaceId = city.PlaceId;

				_scheduleService.UpdateSchedule(schedule);

				var message = cityChanged
					? "City updated successfully. Previous residence and activities have been cleared."
					: "City saved successfully.";

				return Ok(new { message = message, cityChanged = cityChanged });
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Internal server error: {ex.Message}");
			}
		}

		public class ResidenceViewModel
		{
			public int ScheduleId { get; set; }
			public string? HotelId { get; set; }
			public string? HotelName { get; set; }
		}

		[HttpPost]
		public IActionResult SaveResidence([FromBody] ResidenceViewModel vm)
		{
			if (vm == null || vm.ScheduleId <= 0)
				return BadRequest("Invalid payload.");

			var schedule = _scheduleService.GetScheduleById(vm.ScheduleId);
			if (schedule == null)
				return NotFound("Schedule not found.");

			schedule.HotelId = vm.HotelId;
			schedule.HotelName = vm.HotelName;

			_scheduleService.UpdateSchedule(schedule);
			return Ok();
		}

		public class CityViewModel
		{
			public string Name { get; set; }
			public string PlaceId { get; set; }
			public int ScheduleId { get; set; }
		}

		public class DateRangeViewModel
		{
			public string StartDate { get; set; }
			public string EndDate { get; set; }
		}

		[HttpGet]
		public async Task<IActionResult> GetScheduleActivities(int id)
		{
			List<ScheduleActivity> Emi = _ScheduleActivityProductService.GetScheduleActivityByScheduleId(id);
			return Ok(Emi);
		}

		[HttpPost]
		public async Task<IActionResult> SaveScheduleActivities([FromBody] List<ScheduleActivity> activities)
		{
			if (activities == null || activities.Count == 0)
			{
				return BadRequest("No activities provided.");
			}

			foreach (var activity in activities)
			{
				var scheduleActivity = new ScheduleActivity
				{
					ScheduleId = activity.ScheduleId,
					Name = activity.Name,
					PlaceId = activity.PlaceId,
					Type = activity.Type,
					StartHour = activity.StartHour,
					EndHour = activity.EndHour,
					StartMinute = activity.StartMinute,
					EndMinute = activity.EndMinute,
					StartDate = activity.StartDate,
					EndDate = activity.EndDate,
					AddInfo = activity.AddInfo,
					Available = true
				};

				_ScheduleActivityProductService.CreateScheduleActivity(scheduleActivity);
			}
			return Ok(new { message = "Activities saved successfully." });
		}

		[HttpPost]
		public async Task<IActionResult> CreateScheduleActivity([FromBody] ScheduleActivity activity)
		{
			if (activity == null)
				return BadRequest("Invalid activity.");

			_ScheduleActivityProductService.CreateScheduleActivity(activity);
			return Ok(new { scheduleActivity = activity });
		}

		[HttpPut]
		public async Task<IActionResult> UpdateScheduleActivity([FromBody] ScheduleActivity activity)
		{
			if (activity == null || activity.ScheduleActivityId <= 0)
				return BadRequest("Invalid activity.");

			_ScheduleActivityProductService.UpdateScheduleActivity(activity);
			return Ok(new { scheduleActivity = activity });
		}

		[HttpDelete]
		public async Task<IActionResult> DeleteScheduleActivity(int id)
		{
			if (id <= 0)
				return BadRequest("Invalid activity ID.");
			ScheduleActivity activity = _ScheduleActivityProductService.GetScheduleActivityById(id);
			_ScheduleActivityProductService.DeleteScheduleActivity(activity);

			return Ok(new { success = true });
		}

		[HttpDelete]
		public async Task<IActionResult> DeleteSchedule(int id)
		{
			Schedule schedule = _scheduleService.GetScheduleById(id);
			_scheduleService.DeleteSchedule(schedule);

			return Ok(new { success = true });
		}

		[HttpPost]
		public async Task<IActionResult> ExportToGoogleCalendar(int scheduleId)
		{
			try
			{
				var user = _userService.GetUserByName(User.Identity.Name);
				if (user == null)
				{
					return NotFound("User not found.");
				}

				var schedule = _scheduleService.GetScheduleById(scheduleId);
				if (schedule == null)
				{
					return NotFound("Schedule not found.");
				}

				if (schedule.UserId != user.Id)
				{
					return Unauthorized("You don't have permission to export this schedule.");
				}

				var hasValidToken = await _googleCalendarService.HasValidGoogleTokenAsync(user.Id);

				if (!hasValidToken)
				{
					TempData["ExportScheduleId"] = scheduleId;

					var properties = new AuthenticationProperties
					{
						RedirectUri = Url.Action("GoogleCalendarAuthCallback", "Home"),
						Items = { { "scheduleId", scheduleId.ToString() } }
					};

					return Challenge(properties, "Google");
				}

				var activities = _ScheduleActivityProductService.GetScheduleActivityByScheduleId(scheduleId);

				if (activities == null || activities.Count == 0)
				{
					TempData["ErrorMessage"] = "No activities found in this schedule.";
					return RedirectToAction("ViewSchedule", new { id = scheduleId });
				}

				var success = await _googleCalendarService.CreateScheduleEvents(user.Id, schedule, activities);

				if (success)
				{
					TempData["SuccessMessage"] = "Schedule successfully exported to Google Calendar!";
				}
				else
				{
					TempData["ErrorMessage"] = "Failed to export schedule to Google Calendar. Please try again.";
				}

				return RedirectToAction("ViewSchedule", new { id = scheduleId });
			}
			catch (Exception ex)
			{
				TempData["ErrorMessage"] = $"An error occurred while exporting to Google Calendar: {ex.Message}";
				return RedirectToAction("ViewSchedule", new { id = scheduleId });
			}
		}

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> GoogleCalendarAuthCallback()
		{
			try
			{
				var scheduleId = TempData["ExportScheduleId"] as int?;

				if (!scheduleId.HasValue)
				{
					TempData["ErrorMessage"] = "Schedule information was lost during authentication.";
					return RedirectToAction("SchedulesList", new { id = User.Identity.Name });
				}

				var user = _userService.GetUserByName(User.Identity.Name);
				if (user == null)
				{
					return NotFound("User not found.");
				}

				var schedule = _scheduleService.GetScheduleById(scheduleId.Value);
				if (schedule == null)
				{
					return NotFound("Schedule not found.");
				}

				var activities = _ScheduleActivityProductService.GetScheduleActivityByScheduleId(scheduleId.Value);

				await Task.Delay(1000);

				var success = await _googleCalendarService.CreateScheduleEvents(user.Id, schedule, activities);

				if (success)
				{
					TempData["SuccessMessage"] = "Schedule successfully exported to Google Calendar!";
				}
				else
				{
					TempData["ErrorMessage"] = "Failed to export schedule to Google Calendar. Please try again.";
				}

				return RedirectToAction("ViewSchedule", new { id = scheduleId.Value });
			}
			catch (Exception ex)
			{
				TempData["ErrorMessage"] = "An error occurred after Google authentication.";
				return RedirectToAction("Index");
			}
		}
	}
}