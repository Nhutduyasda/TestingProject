using MyProject.Areas.Admin.Models;

namespace MyProject.Interface
{
    public interface IOrderAuditService
    {
        Task LogStatusChangeAsync(int invoiceId, OrderStatus oldStatus, OrderStatus newStatus, string? reason = null, int? userId = null, string? role = null);
        Task<IEnumerable<OrderAuditLog>> GetLogsByInvoiceIdAsync(int invoiceId);
    }
}
