using MyProject.Models.Shared;

namespace MyProject.Interface
{
    public interface IVariantService
    {
        Task<IEnumerable<Variant>> GetAllAsync();
        Task<Variant?> GetByIdAsync(int id);
        Task<IEnumerable<Variant>> GetByProductIdAsync(int productId);
        Task Add(Variant variant);
        Task Update(Variant variant);
        Task Delete(int id);
        Task<bool> IsInStockAsync(int variantId);
        Task UpdateStockAsync(int variantId, int quantity);
    }
}
