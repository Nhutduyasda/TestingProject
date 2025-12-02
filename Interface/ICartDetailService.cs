using MyProject.Areas.User.Models;

namespace MyProject.Interface
{
    public interface ICartDetailService
    {
        Task<IEnumerable<CartDetail>> GetAllAsync();
        Task<CartDetail?> GetByIdAsync(int id);
        Task<IEnumerable<CartDetail>> GetByCartIdAsync(int cartId);
        Task<decimal> GetTotalPriceByCartIdAsync(int cartId);
        Task<int> GetTotalItemsByCartIdAsync(int cartId);
        Task Add(CartDetail cartDetail);
        Task Update(CartDetail cartDetail);
        Task Delete(int id);
    }
}
