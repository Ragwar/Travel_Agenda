using TravelAgenda.Data;
using TravelAgenda.Models;
using TravelAgenda.Repositories.Interfaces;

namespace TravelAgenda.Repositories.Interfaces
{
    public class FavoritesRepository : RepositoryBase<Favorites>, IFavoritesRepository
    {
        public FavoritesRepository(ApplicationDbContext applicationDbContext)
            : base(applicationDbContext)
        {
        }
    }
}
