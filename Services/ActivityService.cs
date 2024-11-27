using TravelAgenda.Models;
using TravelAgenda.Repositories.Interfaces;
using TravelAgenda.Services.Interfaces;

namespace TravelAgenda.Services
{
    public class ActivityService : IActivityService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public ActivityService(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public void CreateActivity(Activity Activity)
        {
            _repositoryWrapper.ActivityRepository.Create(Activity);
            _repositoryWrapper.Save();
        }

        public void DeleteActivity(Activity Activity)
        {
            _repositoryWrapper.ActivityRepository.Delete(Activity);
            _repositoryWrapper.Save();
        }

        public void UpdateActivity(Activity Activity)
        {
            _repositoryWrapper.ActivityRepository.Update(Activity);
            _repositoryWrapper.Save();
        }

        public Activity GetActivityById(int id)
        {
            return _repositoryWrapper.ActivityRepository.FindByCondition(c => c.Activity_Id == id).FirstOrDefault()!;
        }

        public List<Activity> GetActivityByName(string Name)
        {
            return _repositoryWrapper.ActivityRepository.FindAll().ToList();//nu e functia buna, trebuie facuta
        }

        public List<Activity> GetCategories()
        {
            return _repositoryWrapper.ActivityRepository.FindAll().ToList();
        }


    }
}
