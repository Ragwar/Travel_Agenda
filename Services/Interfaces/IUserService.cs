using Microsoft.AspNetCore.Identity;
using TravelAgenda.Models;

namespace TravelAgenda.Services.Interfaces
{
    public interface IUserService
    {
        void CreateUser(IdentityUser User);

        void DeleteUser(IdentityUser User);

        void UpdateUser(IdentityUser User);

        IdentityUser GetUserById(string id);

        IdentityUser GetUserByName(string Name);

        List<IdentityUser> GetUsers();
    }
}
