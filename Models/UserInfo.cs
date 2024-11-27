using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TravelAgenda.Models
{
    public class UserInfo
    {
        [Key]
        public int UserInfo_Id { get; set; }
        public string? Username { get; set; }
        public string? User_Id { get; set; }

        [ForeignKey("User_Id")]
        public IdentityUser? User { get; set; }

    }
}
