namespace TravelAgenda.Repositories.Interfaces
{
    public interface IRepositoryWrapper
    { 
        IScheduleRepository ScheduleRepository { get; }
        IScheduleActivityRepository ScheduleActivityRepository { get; }   
        IUserInfoRepository UserInfoRepository { get; }
        IUserRepository UserRepository { get; }
        void Save();
    }
}
