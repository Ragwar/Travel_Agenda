using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using TravelAgenda.Data;
using TravelAgenda.Models;

namespace TravelAgenda.Services
{
	public interface IGoogleTokenService
	{
		Task<string> GetValidAccessTokenAsync(string userId);
		Task<bool> RefreshAccessTokenAsync(string userId);
	}

	public class GoogleTokenService : IGoogleTokenService
	{
		private readonly ApplicationDbContext _context;
		private readonly IConfiguration _configuration;
		private readonly ILogger<GoogleTokenService> _logger;

		public GoogleTokenService(
			ApplicationDbContext context,
			IConfiguration configuration,
			ILogger<GoogleTokenService> logger)
		{
			_context = context;
			_configuration = configuration;
			_logger = logger;
		}

		public async Task<string> GetValidAccessTokenAsync(string userId)
		{
			var tokenRecord = await _context.UserGoogleTokens
				.FirstOrDefaultAsync(t => t.UserId == userId);

			if (tokenRecord == null)
			{
				_logger.LogWarning($"No token record found for user {userId}");
				return null;
			}

			// Check if token is expired
			if (tokenRecord.ExpiresAt <= DateTime.UtcNow.AddMinutes(-5)) // 5 minute buffer
			{
				_logger.LogInformation($"Token expired for user {userId}, attempting refresh");
				var refreshed = await RefreshAccessTokenAsync(userId);
				if (!refreshed)
				{
					return null;
				}

				// Reload the token after refresh
				tokenRecord = await _context.UserGoogleTokens
					.FirstOrDefaultAsync(t => t.UserId == userId);
			}

			return tokenRecord?.AccessToken;
		}

		public async Task<bool> RefreshAccessTokenAsync(string userId)
		{
			var tokenRecord = await _context.UserGoogleTokens
				.FirstOrDefaultAsync(t => t.UserId == userId);

			if (tokenRecord == null || string.IsNullOrEmpty(tokenRecord.RefreshToken))
			{
				_logger.LogError($"No refresh token available for user {userId}");
				return false;
			}

			try
			{
				var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
				{
					ClientSecrets = new Google.Apis.Auth.OAuth2.ClientSecrets
					{
						ClientId = _configuration["GoogleKeys:ClientId"],
						ClientSecret = _configuration["GoogleKeys:ClientSecret"]
					},
					Scopes = new[] {
						"https://www.googleapis.com/auth/calendar",
						"https://www.googleapis.com/auth/calendar.events"
					}
				});

				var tokenResponse = await flow.RefreshTokenAsync(
					userId,
					tokenRecord.RefreshToken,
					System.Threading.CancellationToken.None);

				if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
				{
					// Update the stored token
					tokenRecord.AccessToken = tokenResponse.AccessToken;
					tokenRecord.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds ?? 3600);
					tokenRecord.UpdatedAt = DateTime.UtcNow;

					await _context.SaveChangesAsync();
					_logger.LogInformation($"Successfully refreshed token for user {userId}");
					return true;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to refresh token for user {userId}");
			}

			return false;
		}
	}
}