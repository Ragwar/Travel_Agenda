using TravelAgenda.Models;

namespace TravelAgenda.Services.Interfaces
{
    public interface IFavoritesService
    {
        void CreateFavorites(Favorites Favorites);

        void DeleteFavorites(Favorites Favorites);

        void UpdateFavorites(Favorites Favorites);

        Favorites GetFavoritesById(int id);

        List<Favorites> GetCategories();
    }
}
