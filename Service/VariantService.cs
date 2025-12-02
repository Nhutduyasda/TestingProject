using Microsoft.EntityFrameworkCore;
using MyProject.Data;
using MyProject.Interface;
using MyProject.Models.Shared;

namespace MyProject.Service
{
    public class VariantService : IVariantService
    {
        private readonly ApplicationDbContext _context;

        public VariantService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Variant>> GetAllAsync()
        {
            return await _context.Variants
                .Include(v => v.Product)
                .ToListAsync();
        }

        public async Task<Variant?> GetByIdAsync(int id)
        {
            return await _context.Variants
                .Include(v => v.Product)
                .Include(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeType)
                .FirstOrDefaultAsync(v => v.VariantId == id);
        }

        public async Task<IEnumerable<Variant>> GetByProductIdAsync(int productId)
        {
            return await _context.Variants
                .Include(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeType)
                .Where(v => v.ProductId == productId)
                .ToListAsync();
        }

        public async Task Add(Variant variant)
        {
            _context.Variants.Add(variant);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Variant variant)
        {
            _context.Variants.Update(variant);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var variant = await _context.Variants.FindAsync(id);
            if (variant != null)
            {
                _context.Variants.Remove(variant);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsInStockAsync(int variantId)
        {
            var variant = await _context.Variants.FindAsync(variantId);
            return variant != null && variant.Quanlity > 0;
        }

        public async Task UpdateStockAsync(int variantId, int quantity)
        {
            var variant = await _context.Variants.FindAsync(variantId);
            if (variant != null)
            {
                variant.Quanlity = quantity;
                await _context.SaveChangesAsync();
            }
        }
    }
}
