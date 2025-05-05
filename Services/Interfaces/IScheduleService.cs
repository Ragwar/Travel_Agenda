using TravelAgenda.Models;

namespace TravelAgenda.Services.Interfaces
{
    public interface IScheduleService
    {
        void CreateSchedule(Schedule Schedule);

        void DeleteSchedule(Schedule Schedule);

        void UpdateSchedule(Schedule Schedule);

        Schedule GetScheduleById(int id);

        public List<Schedule> GetSchedulesByUserId(string id);
        List<Schedule> GetSchedules();
    }
}
