using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravelAgenda.Models;
using System.Security.Claims;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;

namespace TravelAgenda.Services
{
	public interface IGoogleCalendarService
	{
		Task<bool> CreateScheduleEvents(string userId, Schedule schedule, List<Schedule_Activity> activities);
	}

	public class GoogleCalendarService : IGoogleCalendarService
	{
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly UserManager<IdentityUser> _userManager;
		private readonly ILogger<GoogleCalendarService> _logger;
		private readonly IConfiguration _configuration;

		public GoogleCalendarService(
			IHttpContextAccessor httpContextAccessor,
			UserManager<IdentityUser> userManager,
			ILogger<GoogleCalendarService> logger,
			IConfiguration configuration)
		{
			_httpContextAccessor = httpContextAccessor;
			_userManager = userManager;
			_logger = logger;
			_configuration = configuration;
		}

		public async Task<bool> CreateScheduleEvents(string userId, Schedule schedule, List<Schedule_Activity> activities)
		{
			try
			{
				var context = _httpContextAccessor.HttpContext;

				// Get the current user
				var user = await _userManager.FindByIdAsync(userId);
				if (user == null)
				{
					_logger.LogError($"User not found with ID: {userId}");
					return false;
				}

				// Try multiple methods to get the access token
				var accessToken = await GetAccessTokenAsync(context);

				if (string.IsNullOrEmpty(accessToken))
				{
					_logger.LogError("No access token found for user");
					return false;
				}

				_logger.LogInformation($"Access token retrieved successfully");

				// Create credentials using the access token
				var credential = GoogleCredential.FromAccessToken(accessToken);

				// Create Calendar API service
				var service = new CalendarService(new BaseClientService.Initializer()
				{
					HttpClientInitializer = credential,
					ApplicationName = "TravelAgenda",
				});

				// Test the connection first
				try
				{
					var calendarListRequest = service.CalendarList.List();
					var calendars = await calendarListRequest.ExecuteAsync();
					_logger.LogInformation($"Successfully connected to Google Calendar. Found {calendars.Items?.Count ?? 0} calendars.");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to connect to Google Calendar API");
					return false;
				}

				// Group activities by date
				var activitiesByDate = activities
					.Where(a => a.Start_Date.HasValue)
					.GroupBy(a => a.Start_Date.Value.Date)
					.OrderBy(g => g.Key);

				int successCount = 0;
				int totalCount = 0;

				// Create events for each activity
				foreach (var dayActivities in activitiesByDate)
				{
					foreach (var activity in dayActivities.OrderBy(a => a.Start_Hour))
					{
						totalCount++;
						var calendarEvent = CreateCalendarEvent(activity, schedule);

						if (calendarEvent != null)
						{
							try
							{
								var request = service.Events.Insert(calendarEvent, "primary");
								var createdEvent = await request.ExecuteAsync();
								successCount++;
								_logger.LogInformation($"Created event: {createdEvent.Summary} on {createdEvent.Start.DateTime}");
							}
							catch (Exception ex)
							{
								_logger.LogError(ex, $"Failed to create event for activity: {activity.Name}");
								// Continue with other events even if one fails
							}
						}
					}
				}

				_logger.LogInformation($"Successfully created {successCount} out of {totalCount} events");
				return successCount > 0;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to create calendar events");
				return false;
			}
		}

		private async Task<string> GetAccessTokenAsync(HttpContext context)
		{
			try
			{
				// Method 1: Try to get from the Google scheme specifically
				var googleToken = await context.GetTokenAsync("Google", "access_token");
				if (!string.IsNullOrEmpty(googleToken))
				{
					_logger.LogInformation("Access token retrieved from Google scheme");
					return googleToken;
				}

				// Method 2: Try to get from HttpContext.GetTokenAsync (default scheme)
				var token = await context.GetTokenAsync("access_token");
				if (!string.IsNullOrEmpty(token))
				{
					_logger.LogInformation("Access token retrieved from default scheme");
					return token;
				}

				// Method 3: Try to get from the authentication properties
				var authenticateResult = await context.AuthenticateAsync("Google");
				if (authenticateResult.Succeeded)
				{
					var accessToken = authenticateResult.Properties.GetTokenValue("access_token");
					if (!string.IsNullOrEmpty(accessToken))
					{
						_logger.LogInformation("Access token retrieved from Google authentication properties");
						return accessToken;
					}
				}

				// Method 4: Check if we need to refresh the token
				var refreshToken = await context.GetTokenAsync("Google", "refresh_token");
				if (!string.IsNullOrEmpty(refreshToken))
				{
					_logger.LogInformation("Refresh token available - implementing refresh logic would be beneficial");
					// TODO: Implement token refresh logic here if needed
				}

				_logger.LogError("Failed to retrieve access token using any method");
				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Exception occurred while retrieving access token");
				return null;
			}
		}
		private Event CreateCalendarEvent(Schedule_Activity activity, Schedule schedule)
		{
			if (!activity.Start_Date.HasValue)
				return null;

			try
			{
				// Create start and end DateTime objects
				var startDate = activity.Start_Date.Value.Date;
				var startDateTime = startDate.AddHours(activity.Start_Hour ?? 0).AddMinutes(activity.Start_Minute ?? 0);

				var endDateTime = startDate.AddHours(activity.End_Hour ?? activity.Start_Hour ?? 0)
										  .AddMinutes(activity.End_Minute ?? activity.Start_Minute ?? 0);

				// If end time is before start time, assume it's the next day
				if (endDateTime <= startDateTime)
				{
					endDateTime = endDateTime.AddDays(1);
				}

				var calendarEvent = new Event
				{
					Summary = activity.Name,
					Description = $"Activity Type: {activity.Type}\n" +
								  $"Trip: {schedule.City_Name}\n" +
								  $"{(!string.IsNullOrEmpty(activity.Add_Info) ? $"Additional Info: {activity.Add_Info}" : "")}",
					Start = new EventDateTime
					{
						DateTime = startDateTime,
						TimeZone = "Europe/Bucharest" // Use Romania timezone since user is in Craiova
					},
					End = new EventDateTime
					{
						DateTime = endDateTime,
						TimeZone = "Europe/Bucharest"
					}
				};

				// Add location if we have a Google Places ID
				if (!string.IsNullOrEmpty(activity.Place_Id))
				{
					// The location will be the activity name which should include the address
					calendarEvent.Location = activity.Name;

					// Add a link to Google Maps in the description
					calendarEvent.Description += $"\n\nView on Google Maps: https://www.google.com/maps/place/?q=place_id:{activity.Place_Id}";
				}

				// Set reminders (optional - 30 minutes before)
				calendarEvent.Reminders = new Event.RemindersData
				{
					UseDefault = false,
					Overrides = new List<EventReminder>
					{
						new EventReminder { Method = "popup", Minutes = 30 },
						new EventReminder { Method = "email", Minutes = 60 }
					}
				};

				// Color code by activity type
				calendarEvent.ColorId = GetColorIdByType(activity.Type);

				return calendarEvent;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to create calendar event for activity: {activity.Name}");
				return null;
			}
		}

		private string GetColorIdByType(string type)
		{
			// Google Calendar color IDs (1-11)
			// https://developers.google.com/calendar/api/v3/reference/colors/get
			return type?.ToLower() switch
			{
				"restaurant" => "10", // Green
				"hotel" => "9",       // Blue
				"park" => "2",        // Green
				"museum" => "5",      // Yellow
				"shopping_mall" => "6", // Orange
				_ => "7"              // Default - Cyan
			};
		}
	}
}