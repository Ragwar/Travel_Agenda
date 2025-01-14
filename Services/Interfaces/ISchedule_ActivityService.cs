using TravelAgenda.Models;

namespace TravelAgenda.Services.Interfaces
{
    public interface ISchedule_ActivityService
    {
        void CreateSchedule_Activity(Schedule_Activity Schedule_Activity);

        void DeleteSchedule_Activity(Schedule_Activity Schedule_Activity);

        void UpdateSchedule_Activity(Schedule_Activity Schedule_Activity);

        Schedule_Activity GetSchedule_ActivityById(int id);

        List<Schedule_Activity> GetSchedule_Activities();
    }
}
