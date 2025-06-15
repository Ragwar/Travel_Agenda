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
        private readonly string _googleApiKey;
        private readonly ChatClient _chatClient;
		private readonly IGoogleCalendarService _googleCalendarService;


		public HomeController(ApplicationDbContext context, 
            IScheduleService scheduleService, 
            IScheduleActivityService ScheduleActivityProductService, 
            IUserInfoService userInfoService, 
            IUserService userService, 
            IConfiguration configuration, 
			IGoogleCalendarService googleCalendarService
            )

        {
            _scheduleService = scheduleService;
            _ScheduleActivityProductService = ScheduleActivityProductService;
            _userInfoService = userInfoService;
            _userService = userService;
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
				// Optionally store the scheduleId in ViewData, ViewBag, or Session
				ViewBag.ScheduleId = scheduleId.Value;

				// Alternatively, handle any logic here based on the scheduleId
				var schedule = _scheduleService.GetScheduleById(scheduleId.Value);
				if (schedule == null)
				{
					return NotFound("Schedule not found.");
				}

				ViewBag.Schedule = schedule; // Pass the schedule to the view
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

			// Parse the date parameter
			if (!DateTime.TryParse(date, out var selectedDate))
			{
				return BadRequest("Invalid date format");
			}

			// Get all activities for this schedule
			var allActivities = _ScheduleActivityProductService.GetScheduleActivityByScheduleId(scheduleId);

			// Filter activities for the selected date
			var dayActivities = allActivities
				.Where(a => a.StartDate.HasValue && a.StartDate.Value.Date == selectedDate.Date)
				.OrderBy(a => a.StartHour + ((a.StartMinute ?? 0) / 60.0))
				.ToList();

			ViewBag.Schedule = schedule;
			ViewBag.DayActivities = dayActivities;
			ViewBag.SelectedDate = selectedDate;
			ViewData["GoogleApiKey"] = _googleApiKey;

			return View();
		}

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

			// Create a new empty Schedule entry
			var schedule = new Schedule
			{
				UserId = userId,
				// Leave dates as null/default for now - they'll be updated later
				StartDate = default(DateTime),
				EndDate = default(DateTime),
				NrDays = 0,
				StartDay = 0,
				EndDay = 0,
				StartMonth = 0,
				EndMonth = 0
			};

			// Save to database via the ScheduleService
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

			// Parse the dates
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
				// Update existing schedule
				schedule = _scheduleService.GetScheduleById(dateRange.ScheduleId);
				if (schedule == null)
				{
					return NotFound("Schedule not found.");
				}

				// Verify the schedule belongs to the current user
				if (schedule.UserId != user.Id)
				{
					return Unauthorized("You don't have permission to modify this schedule.");
				}
			}
			else
			{
				// Create new schedule with NULL dates initially
				schedule = new Schedule
				{
					UserId = user.Id,
					StartDate = null,    // Changed: Set to null instead of default(DateTime)
					EndDate = null,      // Changed: Set to null instead of default(DateTime)
					NrDays = null,       // Changed: Set to null instead of 0
					StartDay = null,     // Changed: Set to null instead of 0
					EndDay = null,       // Changed: Set to null instead of 0
					StartMonth = null,   // Changed: Set to null instead of 0
					EndMonth = null      // Changed: Set to null instead of 0
				};

				_scheduleService.CreateSchedule(schedule);
			}

			// Calculate Nr_Days
			var nrDays = (endDate - startDate).Days + 1;

			// Update the schedule with dates
			schedule.StartDate = startDate;
			schedule.EndDate = endDate;
			schedule.NrDays = nrDays;
			schedule.StartDay = startDate.Day;
			schedule.EndDay = endDate.Day;
			schedule.StartMonth = startDate.Month;
			schedule.EndMonth = endDate.Month;

			// Update the schedule in database
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
				// Get the latest schedule for the user
				var schedule = _scheduleService.GetScheduleById(city.ScheduleId);
				if (schedule == null)
				{
					return NotFound("Schedule not found.");
				}

				// Check if the city is being changed (not just being set for the first time)
				bool cityChanged = !string.IsNullOrEmpty(schedule.CityName) &&
								  !string.Equals(schedule.CityName, city.Name, StringComparison.OrdinalIgnoreCase);

				// If city is being changed, clean up residence and activities
				if (cityChanged)
				{
					// Clear residence information since it's no longer valid for the new city
					schedule.HotelId = null;
					schedule.HotelName = null;

					// Get all activities for this schedule and delete them
					// since they're no longer relevant to the new city
					var existingActivities = _ScheduleActivityProductService.GetScheduleActivityByScheduleId(city.ScheduleId);

					if (existingActivities != null && existingActivities.Count > 0)
					{
						foreach (var activity in existingActivities)
						{
							_ScheduleActivityProductService.DeleteScheduleActivity(activity);
						}
					}
				}

				// Update the schedule with new city data
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

                // Save using your schedule activity service
                _ScheduleActivityProductService.CreateScheduleActivity(scheduleActivity);
            }
            return Ok(new { message = "Activities saved successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> CreateScheduleActivity([FromBody] ScheduleActivity activity)
        {
            if (activity == null)
                return BadRequest("Invalid activity.");

            // Save the new activity.
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
            // Assuming your service has a DeleteScheduleActivity(int id) method:
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
				// Get the current user
				var user = _userService.GetUserByName(User.Identity.Name);
				if (user == null)
				{
					return NotFound("User not found.");
				}

				// Get the schedule
				var schedule = _scheduleService.GetScheduleById(scheduleId);
				if (schedule == null)
				{
					return NotFound("Schedule not found.");
				}

				// Verify the schedule belongs to the current user
				if (schedule.UserId != user.Id)
				{
					return Unauthorized("You don't have permission to export this schedule.");
				}

				// Check if user has valid Google token
				var hasValidToken = await _googleCalendarService.HasValidGoogleTokenAsync(user.Id);

				if (!hasValidToken)
				{
					// Store the scheduleId in TempData to use after authentication
					TempData["ExportScheduleId"] = scheduleId;

					// User needs to link their Google account
					var properties = new AuthenticationProperties
					{
						RedirectUri = Url.Action("GoogleCalendarAuthCallback", "Home"),
						Items = { { "scheduleId", scheduleId.ToString() } }
					};

					return Challenge(properties, "Google");
				}

				// Get all activities for this schedule
				var activities = _ScheduleActivityProductService.GetScheduleActivityByScheduleId(scheduleId);

				if (activities == null || activities.Count == 0)
				{
					TempData["ErrorMessage"] = "No activities found in this schedule.";
					return RedirectToAction("ViewSchedule", new { id = scheduleId });
				}

				// Export to Google Calendar
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

		// Add this new action to handle the Google authentication callback
		[HttpGet]
		[Authorize]
		public async Task<IActionResult> GoogleCalendarAuthCallback()
		{
			try
			{
				// Get the scheduleId from TempData
				var scheduleId = TempData["ExportScheduleId"] as int?;

				if (!scheduleId.HasValue)
				{
					TempData["ErrorMessage"] = "Schedule information was lost during authentication.";
					return RedirectToAction("SchedulesList", new { id = User.Identity.Name });
				}

				// Now that the user has authenticated with Google, try to export again
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

				// Small delay to ensure tokens are properly saved
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