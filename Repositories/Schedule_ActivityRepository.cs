using TravelAgenda.Data;
using TravelAgenda.Models;
using TravelAgenda.Repositories.Interfaces;

namespace TravelAgenda.Repositories.Interfaces
{
    public class ScheduleActivityRepository : RepositoryBase<ScheduleActivity>, IScheduleActivityRepository
    {
        public ScheduleActivityRepository(ApplicationDbContext applicationDbContext)
            : base(applicationDbContext)
        {
        }
    }
}
