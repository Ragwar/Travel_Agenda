using Microsoft.AspNetCore.Identity;
using TravelAgenda.Data;
using TravelAgenda.Repositories.Interfaces;

namespace TravelAgenda.Repositories.Interfaces
{
    public class UserRepository : RepositoryBase<IdentityUser>, IUserRepository
    {
        public UserRepository(ApplicationDbContext applicationDbContext)
            : base(applicationDbContext)
        {
        }
    }
}
