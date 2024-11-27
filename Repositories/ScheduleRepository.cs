using TravelAgenda.Data;
using TravelAgenda.Models;
using TravelAgenda.Repositories.Interfaces;

namespace TravelAgenda.Repositories.Interfaces
{
    public class ScheduleRepository : RepositoryBase<Schedule>, IScheduleRepository
    {
        public ScheduleRepository(ApplicationDbContext applicationDbContext)
            : base(applicationDbContext)
        {
        }
    }
}
