namespace TravelAgenda.Repositories.Interfaces
{
    public interface IRepositoryWrapper
    { 
        IScheduleRepository ScheduleRepository { get; }
        ISchedule_ActivityRepository Schedule_ActivityRepository { get; }   
        IUserInfoRepository UserInfoRepository { get; }
        IUserRepository UserRepository { get; }
        void Save();
    }
}
