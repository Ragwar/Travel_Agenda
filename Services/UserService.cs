using Microsoft.AspNetCore.Identity;
using TravelAgenda.Models;
using TravelAgenda.Repositories.Interfaces;
using TravelAgenda.Services.Interfaces;

namespace TravelAgenda.Services
{
    public class UserService : IUserService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public UserService(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public void CreateUser(IdentityUser User)
        {
            _repositoryWrapper.UserRepository.Create(User);
            _repositoryWrapper.Save();
        }

        public void DeleteUser(IdentityUser User)
        {
            _repositoryWrapper.UserRepository.Delete(User);
            _repositoryWrapper.Save();
        }

        public void UpdateUser(IdentityUser User)
        {
            _repositoryWrapper.UserRepository.Update(User);
            _repositoryWrapper.Save();
        }

        public IdentityUser GetUserById(string id)
        {
            return _repositoryWrapper.UserRepository.FindByCondition(c => c.Id == id).FirstOrDefault()!;
        }

        public List<IdentityUser> GetUserByName(string Name)
        {
            return _repositoryWrapper.UserRepository.FindAll().ToList();//nu e functia buna, trebuie facuta
        }

        public List<IdentityUser> GetCategories()
        {
            return _repositoryWrapper.UserRepository.FindAll().ToList();
        }


    }
}
