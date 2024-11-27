using TravelAgenda.Data;
using TravelAgenda.Models;
using TravelAgenda.Repositories.Interfaces;

namespace TravelAgenda.Repositories.Interfaces
{
    public class Schedule_ActivityRepository : RepositoryBase<Schedule_Activity>, ISchedule_ActivityRepository
    {
        public Schedule_ActivityRepository(ApplicationDbContext applicationDbContext)
            : base(applicationDbContext)
        {
        }
    }
}
