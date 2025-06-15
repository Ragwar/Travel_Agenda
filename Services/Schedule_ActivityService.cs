using TravelAgenda.Models;
using TravelAgenda.Repositories.Interfaces;
using TravelAgenda.Services.Interfaces;

namespace TravelAgenda.Services
{
    public class ScheduleActivityService : IScheduleActivityService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public ScheduleActivityService(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public void CreateScheduleActivity(ScheduleActivity ScheduleActivity)
        {
            _repositoryWrapper.ScheduleActivityRepository.Create(ScheduleActivity);
            _repositoryWrapper.Save();
        }

        public void DeleteScheduleActivity(ScheduleActivity ScheduleActivity)
        {
            _repositoryWrapper.ScheduleActivityRepository.Delete(ScheduleActivity);
            _repositoryWrapper.Save();
        }

        public void UpdateScheduleActivity(ScheduleActivity ScheduleActivity)
        {
            _repositoryWrapper.ScheduleActivityRepository.Update(ScheduleActivity);
            _repositoryWrapper.Save();
        }

        public ScheduleActivity GetScheduleActivityById(int id)
        {
            return _repositoryWrapper.ScheduleActivityRepository.FindByCondition(c => c.ScheduleActivityId == id).FirstOrDefault()!;
        }

        public List<ScheduleActivity> GetScheduleActivityByName(string Name)
        {
            return _repositoryWrapper.ScheduleActivityRepository.FindAll().ToList();//nu e functia buna, trebuie facuta
        }

        public List<ScheduleActivity> GetSchedule_Activities()
        {
            return _repositoryWrapper.ScheduleActivityRepository.FindAll().ToList();
        }
        
        public List<ScheduleActivity> GetScheduleActivityByScheduleId(int id)
        {
            return _repositoryWrapper.ScheduleActivityRepository.FindByCondition(c => c.ScheduleId == id).ToList();
        }

    }
}
