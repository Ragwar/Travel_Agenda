using TravelAgenda.Models;
using TravelAgenda.Repositories.Interfaces;
using TravelAgenda.Services.Interfaces;

namespace TravelAgenda.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public ScheduleService(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public void CreateSchedule(Schedule Schedule)
        {
            _repositoryWrapper.ScheduleRepository.Create(Schedule);
            _repositoryWrapper.Save();
        }

        public void DeleteSchedule(Schedule Schedule)
        {
            _repositoryWrapper.ScheduleRepository.Delete(Schedule);
            _repositoryWrapper.Save();
        }

        public void UpdateSchedule(Schedule Schedule)
        {
            _repositoryWrapper.ScheduleRepository.Update(Schedule);
            _repositoryWrapper.Save();
        }

        public Schedule GetScheduleById(int id)
        {
            return _repositoryWrapper.ScheduleRepository.FindByCondition(c => c.Schedule_Id == id).FirstOrDefault()!;
        }

        public List<Schedule> GetSchedulesByUserId(string id)
        {
            return _repositoryWrapper.ScheduleRepository.FindByCondition(c => c.User_Id == id).ToList();
        }

        public List<Schedule> GetScheduleByName(string Name)
        {
            return _repositoryWrapper.ScheduleRepository.FindAll().ToList();//nu e functia buna, trebuie facuta
        }

        public List<Schedule> GetSchedules()
        {
            return _repositoryWrapper.ScheduleRepository.FindAll().ToList();
        }


    }
}
