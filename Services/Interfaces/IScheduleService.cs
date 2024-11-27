using TravelAgenda.Models;

namespace TravelAgenda.Services.Interfaces
{
    public interface IScheduleService
    {
        void CreateSchedule(Schedule Schedule);

        void DeleteSchedule(Schedule Schedule);

        void UpdateSchedule(Schedule Schedule);

        Schedule GetScheduleById(int id);

        List<Schedule> GetCategories();
    }
}
