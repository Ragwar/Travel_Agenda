using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TravelAgenda.Models
{
    public class UserInfo
    {
        [Key]
        public int UserInfoId { get; set; }
        public string? Username { get; set; }
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

    }
}
