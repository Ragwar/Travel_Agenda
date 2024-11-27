using TravelAgenda.Data;
using TravelAgenda.Models;
using TravelAgenda.Repositories.Interfaces;

namespace TravelAgenda.Repositories.Interfaces
{
    public class UserInfoRepository : RepositoryBase<UserInfo>, IUserInfoRepository
    {
        public UserInfoRepository(ApplicationDbContext applicationDbContext)
            : base(applicationDbContext)
        {
        }
    }
}
