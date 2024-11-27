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
        public DbSet<Activity>? Activities { get; set; }
    
        public DbSet<Favorites>? Reviews { get; set; }
        public DbSet<Schedule_Activity>? Day_Activities { get; set; }
        public DbSet<Schedule>? Schedules { get; set; }

        public DbSet<UserInfo>? UserInfo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Call base method

            // Your fluent modeling here
            // For example, if you need to specify configurations for your entities:
            // modelBuilder.Entity<Document>().Property(d => d.Name).IsRequired();
            // modelBuilder.Entity<Grade>().HasMany(g => g.Students).WithOne(s => s.Grade);
            // Add any additional configurations for your entities here
       }


    }
}
