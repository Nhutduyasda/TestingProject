using MyProject.Areas.User.Models;

namespace MyProject.Interface
{
    public interface IWishlistService
    {
        Task<IEnumerable<Wishlist>> GetByUserIdAsync(int userId);
        Task AddAsync(int userId, int productId);
        Task RemoveAsync(int userId, int productId);
        Task<bool> IsInWishlistAsync(int userId, int productId);
    }
}
