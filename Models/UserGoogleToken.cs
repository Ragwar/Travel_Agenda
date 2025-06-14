using System.ComponentModel.DataAnnotations;

namespace TravelAgenda.Models
{
	public class UserGoogleToken
	{
		public int Id { get; set; }
		public string UserId { get; set; }
		public string AccessToken { get; set; }
		public string RefreshToken { get; set; }
		public DateTime ExpiresAt { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}
}
