using TravelAgenda.Models;

namespace TravelAgenda.Services.Interfaces
{
    public interface IActivityService
    {
        void CreateActivity(Activity Activity);

        void DeleteActivity(Activity Activity);

        void UpdateActivity(Activity Activity);

        Activity GetActivityById(int id);

        List<Activity> GetActivities();
    }
}
