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

        public IActionResult Index()
        {
            ViewData["GoogleApiKey"] = _googleApiKey;
            ViewBag.Schedules= _scheduleService.GetSchedules();
            return View();
        }

        public IActionResult ActivitiesAndLocations(string cityName, string placeId, double lat, double lng, int scheduleId)
        {
            ViewData["CityName"] = cityName;
            ViewData["PlaceId"] = placeId;
            ViewData["Latitude"] = lat;
            ViewData["Longitude"] = lng;
            ViewData["ScheduleId"] = scheduleId;

            // Optionally, you can fetch related data using scheduleId or placeId here.

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


        // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        //  public IActionResult Error()
        //  {
        //      return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        //  }
    }
}
