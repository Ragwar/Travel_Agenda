using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAgenda.Models
{
    public class Schedule
    {
        [Key]
        public int ScheduleId { get; set; }
        public string ? ScheduleName { get; set; }
        public int? NrDays { get; set; }
        public int? StartDay { get; set; }
        public int? EndDay { get; set; }
        public int? StartMonth { get; set; }
        public int? EndMonth { get; set;}
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? UserId { get; set; }
        public string? CityName { get; set; }
        public string? PlaceId { get; set; }
        public string? HotelName { get; set; }
        public string? HotelId { get; set; }
        public double? ResidenceLat { get; set; }
        public double? ResidenceLng { get; set; }
        public string? ResidenceAddress { get; set; }


        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

    }
}
