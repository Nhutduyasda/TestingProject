using Microsoft.EntityFrameworkCore;
using MyProject.Areas.Admin.Models;
using MyProject.Data;
using MyProject.Interface;

namespace MyProject.Service
{
    public class OrderAuditService : IOrderAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public OrderAuditService(ApplicationDbContext context, IHttpContextAccessor? httpContextAccessor = null)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogStatusChangeAsync(int invoiceId, OrderStatus oldStatus, OrderStatus newStatus, string? reason = null, int? userId = null, string? role = null)
        {
            var auditLog = new OrderAuditLog
            {
                InvoiceId = invoiceId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = userId,
                ChangedByRole = role,
                Reason = reason,
                IpAddress = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                IsAutomated = false
            };

            _context.OrderAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<OrderAuditLog>> GetLogsByInvoiceIdAsync(int invoiceId)
        {
            return await _context.OrderAuditLogs
                .Include(l => l.User) // Assuming ChangedByUserId maps to User if needed, though OrderAuditLog might not have direct nav prop for User based on previous grep
                .Where(l => l.InvoiceId == invoiceId)
                .OrderByDescending(l => l.ChangedAt)
                .ToListAsync();
        }
    }
}
