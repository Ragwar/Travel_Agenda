using TravelAgenda.Models;

namespace TravelAgenda.Services.Interfaces
{
    public interface IScheduleActivityService
    {
        void CreateScheduleActivity(ScheduleActivity ScheduleActivity);

        void DeleteScheduleActivity(ScheduleActivity ScheduleActivity);

        void UpdateScheduleActivity(ScheduleActivity ScheduleActivity);

        ScheduleActivity GetScheduleActivityById(int id);

        List<ScheduleActivity> GetSchedule_Activities();
        List<ScheduleActivity> GetScheduleActivityByScheduleId(int id);
    }
}
