using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAgenda.Models
{
    public class ScheduleActivity
    {
        [Key]
        public int ScheduleActivityId { get; set; }
        public int? StartHour { get; set; }
        public int? EndHour { get; set; }
        public int? StartMinute { get; set; }
        public int? EndMinute { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? AddInfo { get; set; }
        public string? Name { get; set; }
        public string? PlaceId { get; set; }
        public string? Type { get; set; }
        public bool? Available { get; set; }

        // Foreign key to the Day table
        [ForeignKey("Schedule")]
        public int ScheduleId { get; set; }
        public Schedule Schedule { get; set; } // Navigation property
    }
}
