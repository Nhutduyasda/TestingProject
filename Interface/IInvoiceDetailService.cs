using MyProject.Areas.User.Models;

namespace MyProject.Interface
{
    public interface IInvoiceDetailService
    {
        Task<IEnumerable<InvoiceDetail>> GetAllAsync();
        Task<InvoiceDetail?> GetByIdAsync(int id);
        Task<IEnumerable<InvoiceDetail>> GetByInvoiceIdAsync(int invoiceId);
        Task<decimal> GetTotalAmountByInvoiceIdAsync(int invoiceId);
        Task Add(InvoiceDetail invoiceDetail);
        Task Update(InvoiceDetail invoiceDetail);
        Task Delete(int id);
        Task DeleteByInvoiceIdAsync(int invoiceId);
    }
}
