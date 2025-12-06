using Microsoft.EntityFrameworkCore;
using MyProject.Areas.Admin.Models;
using MyProject.Data;
using MyProject.Interface;

namespace MyProject.Service
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;

        public InventoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogStockChangeAsync(int variantId, int quantityChange, InventoryAction action, string? reason = null, int? invoiceId = null, int? userId = null)
        {
            var variant = await _context.Variants.FindAsync(variantId);
            if (variant == null) throw new ArgumentException("Variant not found");

            var log = new InventoryLog
            {
                VariantId = variantId,
                Action = action,
                QuantityChange = quantityChange,
                QuantityBefore = variant.Quanlity,
                QuantityAfter = variant.Quanlity + quantityChange,
                Reason = reason,
                InvoiceId = invoiceId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            // Update actual stock
            variant.Quanlity += quantityChange;

            _context.InventoryLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<InventoryLog>> GetLogsByVariantIdAsync(int variantId)
        {
            return await _context.InventoryLogs
                .Include(l => l.User)
                .Include(l => l.Invoice)
                .Where(l => l.VariantId == variantId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<InventoryLog>> GetAllLogsAsync()
        {
             return await _context.InventoryLogs
                .Include(l => l.Variant)
                .ThenInclude(v => v.Product)
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }
    }
}
