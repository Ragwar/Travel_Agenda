using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAgenda.Models
{
    public class Favorites
    {
        [Key]
        public int Favorites_Id { get; set; }

        public string? User_Id { get; set; }

        [ForeignKey("User_Id")]
        public IdentityUser? User { get; set; }
        [ForeignKey("Activity")]
        public int? Activity_Id { get; set; }
        public Activity? Activity { get; set; } // Navigation property
    }
}
