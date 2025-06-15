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

        //ScheduleActivity
        private IScheduleActivityRepository _ScheduleActivityRepository;
        public IScheduleActivityRepository ScheduleActivityRepository
        {
            get
            {
                if (_ScheduleActivityRepository == null)
                {
                    _ScheduleActivityRepository = new ScheduleActivityRepository(_applicationDbContext);
                }
                return _ScheduleActivityRepository;
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
