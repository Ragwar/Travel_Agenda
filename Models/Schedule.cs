using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelAgenda.Models
{
    public class Schedule
    {
        [Key]
        public int Schedule_Id { get; set; }
       // public string ? Schedule_Name { get; set; }
        public int? Nr_Days { get; set; }
        public int? Start_Day { get; set; }
        public int? End_Day { get; set; }
        public int? Start_Month { get; set; }
        public int? End_Month { get; set;}
        public DateTime? Start_Date { get; set; }
        public DateTime? End_Date { get; set; }
        public string? User_Id { get; set; }
        public string? City_Name { get; set; }
        public string? Place_Id { get; set; }
        public string? Hotel_Name { get; set; }
        public string? Hotel_Id { get; set; }
        public double? Residence_Lat { get; set; }
        public double? Residence_Lng { get; set; }
        public string? Residence_Address { get; set; }


        [ForeignKey("User_Id")]
        public IdentityUser? User { get; set; }

    }
}
