using Microsoft.EntityFrameworkCore;
using MyProject.Data;
using MyProject.Interface;
using MyProject.Areas.User.Models;

namespace MyProject.Service
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Cart>> GetAllAsync()
        {
            return await _context.Carts.Include(c => c.CartDetails).ToListAsync();
        }

        public async Task<Cart?> GetByIdAsync(int id)
        {
            return await _context.Carts
                .Include(c => c.CartDetails)
                .FirstOrDefaultAsync(c => c.CartId == id);
        }

        public async Task<Cart?> GetByUserIdAsync(int userId)
        {
            return await _context.Carts
                .Include(c => c.CartDetails!)
                    .ThenInclude(cd => cd.Variant)
                    .ThenInclude(v => v.Product)
                .Include(c => c.CartDetails!)
                    .ThenInclude(cd => cd.Combo)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<Cart> GetOrCreateCartAsync(int userId)
        {
            var cart = await GetByUserIdAsync(userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId, CreatedAt = DateTime.Now };
                await Add(cart);
                cart = await GetByUserIdAsync(userId);
            }
            return cart!;
        }

        public async Task<Cart?> GetCartWithItemsAsync(int userId)
        {
            return await _context.Carts
                .Include(c => c.CartDetails!)
                    .ThenInclude(d => d.Variant)
                    .ThenInclude(v => v.Product)
                .Include(c => c.CartDetails!)
                    .ThenInclude(d => d.Combo)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task AddItemAsync(int userId, int variantId, int quantity)
        {
            await AddItemAsync(userId, variantId, null, quantity);
        }

        public async Task AddItemAsync(int userId, int? variantId, int? comboId, int quantity)
        {
            if (quantity < 1) quantity = 1;
            if (variantId == null && comboId == null) throw new ArgumentException("Either VariantId or ComboId must be provided");

            // Verify item exists
            if (variantId.HasValue)
            {
                var variant = await _context.Variants.FirstOrDefaultAsync(v => v.VariantId == variantId)
                              ?? throw new InvalidOperationException("Variant not found");
            }
            else if (comboId.HasValue)
            {
                var combo = await _context.Combos.FirstOrDefaultAsync(c => c.ComboId == comboId)
                            ?? throw new InvalidOperationException("Combo not found");
                if (!combo.IsActive) throw new InvalidOperationException("Combo is not active");
            }

            var cart = await GetOrCreateCartAsync(userId);
            
            CartDetail? existing = null;
            if (variantId.HasValue)
            {
                existing = await _context.CartDetails
                    .FirstOrDefaultAsync(d => d.CartId == cart.CartId && d.VariantId == variantId);
            }
            else if (comboId.HasValue)
            {
                existing = await _context.CartDetails
                    .FirstOrDefaultAsync(d => d.CartId == cart.CartId && d.ComboId == comboId);
            }

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                var detail = new CartDetail
                {
                    CartId = cart.CartId,
                    VariantId = variantId,
                    ComboId = comboId,
                    Quantity = quantity
                };
                _context.CartDetails.Add(detail);
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveItemAsync(int cartDetailId)
        {
            var detail = await _context.CartDetails.FindAsync(cartDetailId);
            if (detail == null) return;
            _context.CartDetails.Remove(detail);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateQuantityAsync(int cartDetailId, int quantity)
        {
            var detail = await _context.CartDetails.FindAsync(cartDetailId);
            if (detail != null)
            {
                if (quantity <= 0)
                {
                    _context.CartDetails.Remove(detail);
                }
                else
                {
                    detail.Quantity = quantity;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task Add(Cart cart)
        {
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Cart cart)
        {
            _context.Carts.Update(cart);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var cart = await _context.Carts.FindAsync(id);
            if (cart != null)
            {
                _context.Carts.Remove(cart);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartDetails)
                .FirstOrDefaultAsync(c => c.UserId == userId);
                
            if (cart != null && cart.CartDetails != null)
            {
                _context.CartDetails.RemoveRange(cart.CartDetails);
                _context.Carts.Remove(cart);
                await _context.SaveChangesAsync();
            }
        }
    }
}
