using TravelAgenda.Models;
using TravelAgenda.Repositories.Interfaces;
using TravelAgenda.Services.Interfaces;

namespace TravelAgenda.Services
{
    public class Schedule_ActivityService : ISchedule_ActivityService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public Schedule_ActivityService(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public void CreateSchedule_Activity(Schedule_Activity Schedule_Activity)
        {
            _repositoryWrapper.Schedule_ActivityRepository.Create(Schedule_Activity);
            _repositoryWrapper.Save();
        }

        public void DeleteSchedule_Activity(Schedule_Activity Schedule_Activity)
        {
            _repositoryWrapper.Schedule_ActivityRepository.Delete(Schedule_Activity);
            _repositoryWrapper.Save();
        }

        public void UpdateSchedule_Activity(Schedule_Activity Schedule_Activity)
        {
            _repositoryWrapper.Schedule_ActivityRepository.Update(Schedule_Activity);
            _repositoryWrapper.Save();
        }

        public Schedule_Activity GetSchedule_ActivityById(int id)
        {
            return _repositoryWrapper.Schedule_ActivityRepository.FindByCondition(c => c.Schedule_Activity_Id == id).FirstOrDefault()!;
        }

        public List<Schedule_Activity> GetSchedule_ActivityByName(string Name)
        {
            return _repositoryWrapper.Schedule_ActivityRepository.FindAll().ToList();//nu e functia buna, trebuie facuta
        }

        public List<Schedule_Activity> GetSchedule_Activities()
        {
            return _repositoryWrapper.Schedule_ActivityRepository.FindAll().ToList();
        }


    }
}
