using MyProject.Areas.User.Models;

namespace MyProject.Interface
{
    public interface IInvoiceService
    {
        Task<IEnumerable<Invoice>> GetAllAsync();
        Task<IEnumerable<Invoice>> GetByUserIdAsync(int userId);
        Task<Invoice?> GetByIdAsync(int id);
        Task Add(Invoice invoice);
        Task Update(Invoice invoice);
        Task Delete(int id);
        Task<Invoice> CreateFromCartAsync(int cartId, PayMethod? payMethod);
        Task RecalculateTotalAsync(int invoiceId);
        
        // Order workflow methods
        Task<bool> ConfirmAsync(int invoiceId);
        Task<bool> MarkShippedAsync(int invoiceId);
        Task<bool> MarkCompletedByUserAsync(int invoiceId, int userId);
        Task<bool> RequestCancelAsync(int invoiceId, int userId, string? reason);
        Task<bool> AdminCancelAsync(int invoiceId, string? reason);
        Task<bool> ApproveCancelAsync(int invoiceId, string? reason);
        Task<bool> RejectCancelAsync(int invoiceId);
        
        Task<IEnumerable<Invoice>> GetCancelRequestsAsync();
        Task<bool> CanUserCancelAsync(int invoiceId, int userId);
        Task<IEnumerable<Invoice>> GetUserCancelRequestsAsync(int userId);
    }
}
