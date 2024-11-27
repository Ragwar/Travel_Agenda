using TravelAgenda.Data;
using TravelAgenda.Models;
using TravelAgenda.Repositories.Interfaces;

namespace TravelAgenda.Repositories.Interfaces
{
    public class ActivityRepository : RepositoryBase<Activity>, IActivityRepository
    {
        public ActivityRepository(ApplicationDbContext applicationDbContext)
            : base(applicationDbContext)
        {
        }
    }
}
