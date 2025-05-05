using GoogleApi.Entities.Search.Video.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IActivityService _activityService;
        private readonly IScheduleService _scheduleService;
        private readonly ISchedule_ActivityService _schedule_activityProductService;
        private readonly IFavoritesService _favoritesService;
        private readonly IUserInfoService _userInfoService;
        private readonly IUserService _userService;
        private readonly string _googleApiKey;

        public HomeController(ApplicationDbContext context, IActivityService activityService, IScheduleService scheduleService, ISchedule_ActivityService schedule_activityProductService, IUserInfoService userInfoService, IUserService userService, IFavoritesService favoritesService, IConfiguration configuration)
        {
            _activityService = activityService;
            _scheduleService = scheduleService;
            _schedule_activityProductService = schedule_activityProductService;
            _favoritesService = favoritesService;
            _userInfoService = userInfoService;
            _userService = userService;
            _googleApiKey = configuration["GoogleAPI:ApiKey"];
            _context = context; 
        }
        public IActionResult Home()
        {
            return View();
        }

        public IActionResult Index(string id)
        {
            ViewData["GoogleApiKey"] = _googleApiKey;
            ViewBag.Schedules= _scheduleService.GetSchedulesByUserId(id);
            return View();
        }

        public IActionResult SchedulesList(string id)
        {
            ViewData["GoogleApiKey"] = _googleApiKey;
            ViewBag.Schedules = _scheduleService.GetSchedulesByUserId(id);
            return View();
        }

        public IActionResult LocationsAndActivities(int id)
        {
            Schedule schedule = _scheduleService.GetScheduleById(id);
            ViewBag.Schedule = _scheduleService.GetScheduleById(id);
            ViewData["CityName"] = schedule.City_Name;
            ViewData["PlaceId"] = schedule.Place_Id;
            List <Schedule_Activity> Emi = _schedule_activityProductService.GetSchedule_ActivityByScheduleId(id);
            ViewBag.Locations = Emi;
            ViewData["GoogleApiKey"] = _googleApiKey; // Add your API key here

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveDates([FromBody] DateRangeViewModel dateRange)
        {
            if (dateRange == null || string.IsNullOrEmpty(dateRange.StartDate) || string.IsNullOrEmpty(dateRange.EndDate))
            {
                return BadRequest("Invalid date range.");
            }

            // Parse the dates
            if (!DateTime.TryParse(dateRange.StartDate, out var startDate) || !DateTime.TryParse(dateRange.EndDate, out var endDate))
            {
                return BadRequest("Invalid date format.");
            }

            var user =  _userService.GetUserByName(User.Identity.Name);
            if (user == null)
            {
                return NotFound("User not found.");
            }
            var userId = user.Id;

            if (userId == null)
            {
                return Unauthorized("User not logged in.");
            }

            // Calculate Nr_Days
            var nrDays = (endDate - startDate).Days + 1;

            // Create a new Schedule entry
            var schedule = new Schedule
            {
                User_Id = userId,
                Start_Date = startDate,
                End_Date = endDate,
                Nr_Days = nrDays,
                Start_Day = startDate.Day,
                End_Day = endDate.Day,
                Start_Month = startDate.Month,
                End_Month = endDate.Month
            };

            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid model.");
            }
            // Save to database via the ScheduleService
             _scheduleService.CreateSchedule(schedule);
            //_context.Add(schedule);
            //await _context.SaveChangesAsync();

           // return Ok(new { redirectUrl = Url.Action("Activities", "Home") });
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

        public class CityViewModel
        {
            public string Name { get; set; }
            public string PlaceId { get; set; }
            public int ScheduleId { get; set; }
        }


        public IActionResult Privacy()
        {
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
        public class DateRangeViewModel
        {
            public string StartDate { get; set; }
            public string EndDate { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetScheduleActivities(int id)
        {
            // e.g. date in "yyyy-MM-dd" format
            //var all = _schedule_activityProductService
            //              .GetSchedule_ActivityByScheduleId(scheduleId);
            List<Schedule_Activity> Emi = _schedule_activityProductService.GetSchedule_ActivityByScheduleId(id);
            // filter to only those with Start_Date == date
           //   var filtered = Emi
           //       .Where(a => a.Start_Date.ToString("yyyy-MM-dd") == date)
            //      .ToList();

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


    }
}
