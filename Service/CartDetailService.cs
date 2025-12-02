using Microsoft.EntityFrameworkCore;
using MyProject.Data;
using MyProject.Interface;
using MyProject.Areas.User.Models;

namespace MyProject.Service
{
    public class CartDetailService : ICartDetailService
    {
        private readonly ApplicationDbContext _context;

        public CartDetailService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CartDetail>> GetAllAsync()
        {
            return await _context.CartDetails
                .Include(cd => cd.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images)
                .ToListAsync();
        }

        public async Task<CartDetail?> GetByIdAsync(int id)
        {
            return await _context.CartDetails
                .Include(cd => cd.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(cd => cd.CartDetailId == id);
        }

        public async Task<IEnumerable<CartDetail>> GetByCartIdAsync(int cartId)
        {
            return await _context.CartDetails
                .Include(cd => cd.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Images)
                .Where(cd => cd.CartId == cartId)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalPriceByCartIdAsync(int cartId)
        {
            var cartDetails = await GetByCartIdAsync(cartId);
            return cartDetails.Sum(cd => cd.Variant.Price * cd.Quantity);
        }

        public async Task<int> GetTotalItemsByCartIdAsync(int cartId)
        {
            return await _context.CartDetails
                .Where(cd => cd.CartId == cartId)
                .SumAsync(cd => cd.Quantity);
        }

        public async Task Add(CartDetail cartDetail)
        {
            _context.CartDetails.Add(cartDetail);
            await _context.SaveChangesAsync();
        }

        public async Task Update(CartDetail cartDetail)
        {
            _context.CartDetails.Update(cartDetail);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var cartDetail = await _context.CartDetails.FindAsync(id);
            if (cartDetail != null)
            {
                _context.CartDetails.Remove(cartDetail);
                await _context.SaveChangesAsync();
            }
        }
    }
}
