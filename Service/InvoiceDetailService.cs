using Microsoft.EntityFrameworkCore;
using MyProject.Data;
using MyProject.Interface;
using MyProject.Areas.User.Models;

namespace MyProject.Service
{
    public class InvoiceDetailService : IInvoiceDetailService
    {
        private readonly ApplicationDbContext _context;

        public InvoiceDetailService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<InvoiceDetail>> GetAllAsync()
        {
            return await _context.InvoiceDetails
                .Include(id => id.Variant)
                    .ThenInclude(v => v!.Product)
                .ToListAsync();
        }

        public async Task<InvoiceDetail?> GetByIdAsync(int id)
        {
            return await _context.InvoiceDetails
                .Include(idet => idet.Variant)
                    .ThenInclude(v => v!.Product)
                .Include(idet => idet.Combo)
                .FirstOrDefaultAsync(idet => idet.InvoiceDetailId == id);
        }

        public async Task<IEnumerable<InvoiceDetail>> GetByInvoiceIdAsync(int invoiceId)
        {
            return await _context.InvoiceDetails
                .Include(id => id.Variant)
                    .ThenInclude(v => v!.Product)
                .Include(id => id.Combo)
                .Where(id => id.InvoiceId == invoiceId)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalAmountByInvoiceIdAsync(int invoiceId)
        {
            var details = await GetByInvoiceIdAsync(invoiceId);
            return details.Sum(id => id.UnitPrice * id.Quantity);
        }

        public async Task Add(InvoiceDetail invoiceDetail)
        {
            _context.InvoiceDetails.Add(invoiceDetail);
            await _context.SaveChangesAsync();
        }

        public async Task Update(InvoiceDetail invoiceDetail)
        {
            _context.InvoiceDetails.Update(invoiceDetail);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var invoiceDetail = await _context.InvoiceDetails.FindAsync(id);
            if (invoiceDetail != null)
            {
                _context.InvoiceDetails.Remove(invoiceDetail);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteByInvoiceIdAsync(int invoiceId)
        {
            var details = await _context.InvoiceDetails
                .Where(id => id.InvoiceId == invoiceId)
                .ToListAsync();
                
            _context.InvoiceDetails.RemoveRange(details);
            await _context.SaveChangesAsync();
        }
    }
}
