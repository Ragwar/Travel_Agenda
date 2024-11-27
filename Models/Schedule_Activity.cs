using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAgenda.Models
{
    public class Schedule_Activity
    {
        [Key]
        public int Schedule_Activity_Id { get; set; }
        public int? Start_Hour { get; set; }
        public int? End_Hour { get; set; }
        public int? Start_Minute { get; set; }
        public int? End_Minute { get; set; }
        public DateTime? Start_Date { get; set; }
        public DateTime? End_Date { get; set; }
        public string? Add_Info { get; set; }
        public string? Name { get; set; }
        public string? Place_Id { get; set; }
        public string? Type { get; set; }
        public bool? Available { get; set; }

        // Foreign key to the Day table
        [ForeignKey("Schedule")]
        public int Schedule_Id { get; set; }
        public Schedule Schedule { get; set; } // Navigation property

        // Foreign key to the Activity table
        [ForeignKey("Activity")]
        public int Activity_Id { get; set; }
        public Activity Activity { get; set; } // Navigation property
    }
}
