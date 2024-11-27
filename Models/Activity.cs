using System.ComponentModel.DataAnnotations;

namespace TravelAgenda.Models
{
    public class Activity
    {
        [Key]
        public int Activity_Id { get; set; }
        public string? Name { get; set; }
        public string? Place_Id { get; set; }
        public string? Type { get; set; }
        public bool? Available { get; set; }

        // Navigation property for the many-to-many relationship
        public ICollection<Schedule_Activity>? Schedule_Activity { get; set; }

        public ICollection<Favorites>? Favorites { get; set; }
    }
}
