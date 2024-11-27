namespace TravelAgenda.Repositories.Interfaces
{
    public interface IRepositoryWrapper
    {
        IActivityRepository ActivityRepository { get; }
        IScheduleRepository ScheduleRepository { get; }
        ISchedule_ActivityRepository Schedule_ActivityRepository { get; }   
        IUserInfoRepository UserInfoRepository { get; }
        IUserRepository UserRepository { get; }
        IFavoritesRepository FavoritesRepository { get; }
        void Save();
    }
}
