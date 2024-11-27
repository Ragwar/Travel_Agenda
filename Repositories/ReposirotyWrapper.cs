using TravelAgenda.Data;
using TravelAgenda.Repositories.Interfaces;

namespace TravelAgenda.Repositories
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        protected ApplicationDbContext _applicationDbContext { get; set; }
        public RepositoryWrapper(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        public void Save()
        {
            _applicationDbContext.SaveChanges();
        }

        //Activity
        private IActivityRepository _ActivityRepository;
        public IActivityRepository ActivityRepository
        {
            get
            {
                if (_ActivityRepository == null)
                {
                    _ActivityRepository = new ActivityRepository(_applicationDbContext);
                }
                return _ActivityRepository;
            }
        }

        //Favorites
        private IFavoritesRepository _FavoritesRepository;
        public IFavoritesRepository FavoritesRepository
        {
            get
            {
                if (_FavoritesRepository == null)
                {
                    _FavoritesRepository = new FavoritesRepository(_applicationDbContext);
                }
                return _FavoritesRepository;
            }
        }

        //Schedule
        private IScheduleRepository _ScheduleRepository;
        public IScheduleRepository ScheduleRepository
        {
            get
            {
                if (_ScheduleRepository == null)
                {
                    _ScheduleRepository = new ScheduleRepository(_applicationDbContext);
                }
                return _ScheduleRepository;
            }
        }

        //UserInfo
        private IUserInfoRepository _userInfoRepository;
        public IUserInfoRepository UserInfoRepository
        {
            get
            {
                if (_userInfoRepository == null)
                {
                    _userInfoRepository = new UserInfoRepository(_applicationDbContext);
                }
                return _userInfoRepository;
            }
        }

        //Schedule_Activity
        private ISchedule_ActivityRepository _Schedule_ActivityRepository;
        public ISchedule_ActivityRepository Schedule_ActivityRepository
        {
            get
            {
                if (_Schedule_ActivityRepository == null)
                {
                    _Schedule_ActivityRepository = new Schedule_ActivityRepository(_applicationDbContext);
                }
                return _Schedule_ActivityRepository;
            }
        }

       
        //User
        private IUserRepository _userRepository;
        public IUserRepository UserRepository
        {
            get
            {
                if (_userRepository == null)
                {
                    _userRepository = new UserRepository(_applicationDbContext);
                }
                return _userRepository;
            }
        }
    }

}
