using TravelAgenda.Models;
using TravelAgenda.Repositories.Interfaces;
using TravelAgenda.Services.Interfaces;

namespace TravelAgenda.Services
{
    public class FavoritesService : IFavoritesService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public FavoritesService(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public void CreateFavorites(Favorites Favorites)
        {
            _repositoryWrapper.FavoritesRepository.Create(Favorites);
            _repositoryWrapper.Save();
        }

        public void DeleteFavorites(Favorites Favorites)
        {
            _repositoryWrapper.FavoritesRepository.Delete(Favorites);
            _repositoryWrapper.Save();
        }

        public void UpdateFavorites(Favorites Favorites)
        {
            _repositoryWrapper.FavoritesRepository.Update(Favorites);
            _repositoryWrapper.Save();
        }

        public Favorites GetFavoritesById(int id)
        {
            return _repositoryWrapper.FavoritesRepository.FindByCondition(c => c.Favorites_Id == id).FirstOrDefault()!;
        }

        public List<Favorites> GetFavoritesByName(string Name)
        {
            return _repositoryWrapper.FavoritesRepository.FindAll().ToList();//nu e functia buna, trebuie facuta
        }

        public List<Favorites> GetFavorites()
        {
            return _repositoryWrapper.FavoritesRepository.FindAll().ToList();
        }


    }
}
