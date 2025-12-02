using MyProject.Areas.User.Models;

namespace MyProject.Interface
{
    public interface ICartService
    {
        Task<IEnumerable<Cart>> GetAllAsync();
        Task<Cart?> GetByIdAsync(int id);
        Task<Cart?> GetByUserIdAsync(int userId);
        Task<Cart> GetOrCreateCartAsync(int userId);
        Task<Cart?> GetCartWithItemsAsync(int userId);
        Task AddItemAsync(int userId, int variantId, int quantity);
        Task RemoveItemAsync(int cartDetailId);
        Task UpdateQuantityAsync(int cartDetailId, int quantity);
        Task Add(Cart cart);
        Task Update(Cart cart);
        Task Delete(int id);
        Task ClearCartAsync(int userId);
    }
}
