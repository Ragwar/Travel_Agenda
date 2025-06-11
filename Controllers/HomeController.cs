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
        private readonly ISchedule_ActivityService _schedule_activityProductService;
        private readonly IUserInfoService _userInfoService;
        private readonly IUserService _userService;
        private readonly string _googleApiKey;
        private readonly ChatClient _chatClient;
		private readonly IGoogleCalendarService _googleCalendarService;


		public HomeController(ApplicationDbContext context, 
            IScheduleService scheduleService, 
            ISchedule_ActivityService schedule_activityProductService, 
            IUserInfoService userInfoService, 
            IUserService userService, 
            IConfiguration configuration, 
			IGoogleCalendarService googleCalendarService
            )

        {
            _scheduleService = scheduleService;
            _schedule_activityProductService = schedule_activityProductService;
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
            ViewData["CityName"] = schedule.City_Name;
            ViewData["PlaceId"] = schedule.Place_Id;
            ViewData["GoogleApiKey"] = _googleApiKey;

            var activities = _schedule_activityProductService
                                 .GetSchedule_ActivityByScheduleId(scheduleId);
            return View(activities);
        }

        public IActionResult Residence(int scheduleId)
        {
            var schedule = _scheduleService.GetScheduleById(scheduleId);
            if (schedule == null) return NotFound();

            ViewBag.Schedule = schedule;
            ViewData["CityName"] = schedule.City_Name;
            ViewData["PlaceId"] = schedule.Place_Id;
            ViewData["GoogleApiKey"] = _googleApiKey;

            return View();
        }

		public IActionResult ViewSchedule(int id)
		{
			var schedule = _scheduleService.GetScheduleById(id);
			var activities = _schedule_activityProductService.GetSchedule_ActivityByScheduleId(schedule.Schedule_Id);
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
			var allActivities = _schedule_activityProductService.GetSchedule_ActivityByScheduleId(scheduleId);

			// Filter activities for the selected date
			var dayActivities = allActivities
				.Where(a => a.Start_Date.HasValue && a.Start_Date.Value.Date == selectedDate.Date)
				.OrderBy(a => a.Start_Hour + ((a.Start_Minute ?? 0) / 60.0))
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
				User_Id = userId,
				// Leave dates as null/default for now - they'll be updated later
				Start_Date = default(DateTime),
				End_Date = default(DateTime),
				Nr_Days = 0,
				Start_Day = 0,
				End_Day = 0,
				Start_Month = 0,
				End_Month = 0
			};

			// Save to database via the ScheduleService
			_scheduleService.CreateSchedule(schedule);

			return Ok(new { scheduleId = schedule.Schedule_Id });
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
				if (schedule.User_Id != user.Id)
				{
					return Unauthorized("You don't have permission to modify this schedule.");
				}
			}
			else
			{
				// Create new schedule with NULL dates initially
				schedule = new Schedule
				{
					User_Id = user.Id,
					Start_Date = null,    // Changed: Set to null instead of default(DateTime)
					End_Date = null,      // Changed: Set to null instead of default(DateTime)
					Nr_Days = null,       // Changed: Set to null instead of 0
					Start_Day = null,     // Changed: Set to null instead of 0
					End_Day = null,       // Changed: Set to null instead of 0
					Start_Month = null,   // Changed: Set to null instead of 0
					End_Month = null      // Changed: Set to null instead of 0
				};

				_scheduleService.CreateSchedule(schedule);
			}

			// Calculate Nr_Days
			var nrDays = (endDate - startDate).Days + 1;

			// Update the schedule with dates
			schedule.Start_Date = startDate;
			schedule.End_Date = endDate;
			schedule.Nr_Days = nrDays;
			schedule.Start_Day = startDate.Day;
			schedule.End_Day = endDate.Day;
			schedule.Start_Month = startDate.Month;
			schedule.End_Month = endDate.Month;

			// Update the schedule in database
			_scheduleService.UpdateSchedule(schedule);

			return Ok(new { scheduleId = schedule.Schedule_Id });
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

                // Update the schedule with city data
                schedule.City_Name = city.Name;
                schedule.Place_Id = city.PlaceId;

                _scheduleService.UpdateSchedule(schedule);

                return Ok("City saved successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        public class ResidenceViewModel
        {
            public int Schedule_Id { get; set; }
            public string? Hotel_Id { get; set; }
            public string? Hotel_Name { get; set; }
        }

        [HttpPost]
        public IActionResult SaveResidence([FromBody] ResidenceViewModel vm)
        {
            if (vm == null || vm.Schedule_Id <= 0)
                return BadRequest("Invalid payload.");

            var schedule = _scheduleService.GetScheduleById(vm.Schedule_Id);
            if (schedule == null)
                return NotFound("Schedule not found.");

            // always write vm.Hotel_Id / vm.Hotel_Name
            schedule.Hotel_Id = vm.Hotel_Id;
            schedule.Hotel_Name = vm.Hotel_Name;

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
            List<Schedule_Activity> Emi = _schedule_activityProductService.GetSchedule_ActivityByScheduleId(id);
            return Ok(Emi);
        }

        [HttpPost]
        public async Task<IActionResult> SaveScheduleActivities([FromBody] List<Schedule_Activity> activities)
        {
            if (activities == null || activities.Count == 0)
            {
                return BadRequest("No activities provided.");
            }

            foreach (var activity in activities)
            {
                var scheduleActivity = new Schedule_Activity
                {
                    Schedule_Id = activity.Schedule_Id,
                    Name = activity.Name,
                    Place_Id = activity.Place_Id,
                    Type = activity.Type,
                    Start_Hour = activity.Start_Hour,
                    End_Hour = activity.End_Hour,
                    Start_Minute = activity.Start_Minute,
                    End_Minute = activity.End_Minute,
                    Start_Date = activity.Start_Date,
                    End_Date = activity.End_Date,
                    Add_Info = activity.Add_Info,
                    Available = true
                };

                // Save using your schedule activity service
                _schedule_activityProductService.CreateSchedule_Activity(scheduleActivity);
            }
            return Ok(new { message = "Activities saved successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> CreateScheduleActivity([FromBody] Schedule_Activity activity)
        {
            if (activity == null)
                return BadRequest("Invalid activity.");

            // Save the new activity.
            _schedule_activityProductService.CreateSchedule_Activity(activity);
            return Ok(new { scheduleActivity = activity });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateScheduleActivity([FromBody] Schedule_Activity activity)
        {
            if (activity == null || activity.Schedule_Activity_Id <= 0)
                return BadRequest("Invalid activity.");

            _schedule_activityProductService.UpdateSchedule_Activity(activity);
            return Ok(new { scheduleActivity = activity });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteScheduleActivity(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid activity ID.");
            Schedule_Activity activity = _schedule_activityProductService.GetSchedule_ActivityById(id);
            // Assuming your service has a DeleteSchedule_Activity(int id) method:
            _schedule_activityProductService.DeleteSchedule_Activity(activity);

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
		[Authorize] // Use default authorization, not Google-specific
		public async Task<IActionResult> ExportToGoogleCalendar(int scheduleId)
		{
			try
			{
				// Check if user has a valid access token for Google Calendar
				var accessToken = await HttpContext.GetTokenAsync("Google", "access_token");

				if (string.IsNullOrEmpty(accessToken))
				{
					// User needs to link their Google account - redirect to Google OAuth
					var properties = new AuthenticationProperties
					{
						RedirectUri = Url.Action("ExportToGoogleCalendar", new { scheduleId = scheduleId }),
						Items = { { "scheduleId", scheduleId.ToString() } }
					};

					return Challenge(properties, "Google");
				}

				// Get the schedule
				var schedule = _scheduleService.GetScheduleById(scheduleId);
				if (schedule == null)
				{
					return NotFound("Schedule not found.");
				}

				// Verify the schedule belongs to the current user
				var user = _userService.GetUserByName(User.Identity.Name);
				if (user == null || schedule.User_Id != user.Id)
				{
					return Unauthorized("You don't have permission to export this schedule.");
				}

				// Get all activities for this schedule
				var activities = _schedule_activityProductService.GetSchedule_ActivityByScheduleId(scheduleId);

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
	}


}