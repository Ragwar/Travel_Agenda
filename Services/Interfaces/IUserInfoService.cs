using TravelAgenda.Models;

namespace TravelAgenda.Services.Interfaces
{
    public interface IUserInfoService
    {
        void CreateUserInfo(UserInfo UserInfo);

        void DeleteUserInfo(UserInfo UserInfo);

        void UpdateUserInfo(UserInfo UserInfo);

        UserInfo GetUserInfoById(int id);

        List<UserInfo> GetCategories();
    }
}
