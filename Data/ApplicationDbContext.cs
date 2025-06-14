using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TravelAgenda.Models;

namespace TravelAgenda.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        public DbSet<Schedule_Activity>? Day_Activities { get; set; }
        public DbSet<Schedule>? Schedules { get; set; }

        public DbSet<UserInfo>? UserInfo { get; set; }
		public DbSet<UserGoogleToken> UserGoogleTokens { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Call base method
       }


    }
}
