using Microsoft.EntityFrameworkCore;
using MyProject.Areas.User.Models;
using MyProject.Data;
using MyProject.Interface;

namespace MyProject.Service
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;

        public ReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProductReview>> GetByProductIdAsync(int productId)
        {
            return await _context.ProductReviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> CanUserReviewAsync(int userId, int productId)
        {
            // Check if user has purchased the product and order is completed
            var hasPurchased = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Variant)
                .AnyAsync(i => i.UserId == userId 
                            && i.Status == Areas.Admin.Models.OrderStatus.Completed
                            && i.InvoiceDetails.Any(d => d.Variant != null && d.Variant.ProductId == productId));

            // Check if user has already reviewed
            var hasReviewed = await _context.ProductReviews
                .AnyAsync(r => r.UserId == userId && r.ProductId == productId);

            return hasPurchased && !hasReviewed;
        }

        public async Task<ProductReview?> GetUserReviewAsync(int userId, int productId)
        {
            return await _context.ProductReviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);
        }

        public async Task AddReviewAsync(ProductReview review)
        {
            review.CreatedAt = DateTime.UtcNow;
            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateReviewAsync(ProductReview review)
        {
            var existingReview = await _context.ProductReviews.FindAsync(review.ReviewId);
            if (existingReview != null)
            {
                existingReview.Rating = review.Rating;
                existingReview.ReviewText = review.ReviewText;
                existingReview.UpdatedAt = DateTime.UtcNow;
                // Keep IsApproved status or reset it? Let's keep it for now, or reset if we want re-moderation.
                // existingReview.IsApproved = false; 
                
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteReviewAsync(int reviewId)
        {
            var review = await _context.ProductReviews.FindAsync(reviewId);
            if (review != null)
            {
                _context.ProductReviews.Remove(review);
                await _context.SaveChangesAsync();
            }
        }
    }
}
