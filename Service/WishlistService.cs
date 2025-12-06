using Microsoft.EntityFrameworkCore;
using MyProject.Areas.User.Models;
using MyProject.Data;
using MyProject.Interface;

namespace MyProject.Service
{
    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;

        public WishlistService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Wishlist>> GetByUserIdAsync(int userId)
        {
            return await _context.Wishlists
                .Include(w => w.Product)
                .ThenInclude(p => p.Images)
                .Include(w => w.Product)
                .ThenInclude(p => p.Variants)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();
        }

        public async Task AddAsync(int userId, int productId)
        {
            if (await IsInWishlistAsync(userId, productId)) return;

            var wishlist = new Wishlist
            {
                UserId = userId,
                ProductId = productId,
                AddedAt = DateTime.UtcNow
            };

            _context.Wishlists.Add(wishlist);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveAsync(int userId, int productId)
        {
            var item = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (item != null)
            {
                _context.Wishlists.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsInWishlistAsync(int userId, int productId)
        {
            return await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);
        }
    }
}
