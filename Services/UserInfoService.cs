using TravelAgenda.Models;
using TravelAgenda.Repositories.Interfaces;
using TravelAgenda.Services.Interfaces;

namespace TravelAgenda.Services
{
    public class UserInfoService : IUserInfoService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public UserInfoService(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public void CreateUserInfo(UserInfo UserInfo)
        {
            _repositoryWrapper.UserInfoRepository.Create(UserInfo);
            _repositoryWrapper.Save();
        }

        public void DeleteUserInfo(UserInfo UserInfo)
        {
            _repositoryWrapper.UserInfoRepository.Delete(UserInfo);
            _repositoryWrapper.Save();
        }

        public void UpdateUserInfo(UserInfo UserInfo)
        {
            _repositoryWrapper.UserInfoRepository.Update(UserInfo);
            _repositoryWrapper.Save();
        }

        public UserInfo GetUserInfoById(int id)
        {
            return _repositoryWrapper.UserInfoRepository.FindByCondition(c => c.UserInfoId == id).FirstOrDefault()!;
        }

        public List<UserInfo> GetUserInfoByName(string Name)
        {
            return _repositoryWrapper.UserInfoRepository.FindAll().ToList();//nu e functia buna, trebuie facuta
        }

        public List<UserInfo> GetCategories()
        {
            return _repositoryWrapper.UserInfoRepository.FindAll().ToList();
        }


    }
}
