using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TravelAgenda.Models;
using TravelAgenda.Services;
using TravelAgenda.Services.Interfaces;

namespace TravelAgenda.Controllers
{
    public class HomeController : Controller
    {
        private readonly IActivityService _activityService;
        private readonly IScheduleService _scheduleService;
        private readonly ISchedule_ActivityService _schedule_activityProductService;
        private readonly IFavoritesService _favoritesService;
        private readonly IUserInfoService _userInfoService;
        private readonly IUserService _userService;
        private readonly string _googleApiKey;

        public HomeController(IActivityService activityService, IScheduleService scheduleService, ISchedule_ActivityService schedule_activityProductService, IUserInfoService userInfoService, IUserService userService, IFavoritesService favoritesService, IConfiguration configuration)
        {
            _activityService = activityService;
            _scheduleService = scheduleService;
            _schedule_activityProductService = schedule_activityProductService;
            _favoritesService = favoritesService;
            _userInfoService = userInfoService;
            _userService = userService;
            _googleApiKey = configuration["GoogleAPI:ApiKey"];
        }

        public IActionResult Index()
        {
            ViewData["GoogleApiKey"] = _googleApiKey;
            return View();
        }

        [HttpPost]
        public IActionResult SaveDates([FromBody] DateRangeViewModel dateRange)
        {
            if (dateRange == null || string.IsNullOrEmpty(dateRange.StartDate) || string.IsNullOrEmpty(dateRange.EndDate))
            {
                return BadRequest("Invalid date range.");
            }

            // Save dates to a database or perform another action
            Debug.WriteLine($"Start Date: {dateRange.StartDate}, End Date: {dateRange.EndDate}");

            return Ok("Dates saved successfully.");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Activities()
        {
            ViewData["GoogleApiKey"] = _googleApiKey;
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
