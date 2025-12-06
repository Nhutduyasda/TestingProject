using MyProject.Areas.User.Models;

namespace MyProject.Interface
{
    public interface IReviewService
    {
        Task<IEnumerable<ProductReview>> GetByProductIdAsync(int productId);
        Task<bool> CanUserReviewAsync(int userId, int productId);
        Task<ProductReview?> GetUserReviewAsync(int userId, int productId);
        Task AddReviewAsync(ProductReview review);
        Task UpdateReviewAsync(ProductReview review);
        Task DeleteReviewAsync(int reviewId);
    }
}
