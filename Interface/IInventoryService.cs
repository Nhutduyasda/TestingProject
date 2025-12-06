using MyProject.Areas.Admin.Models;
using MyProject.Models.Shared;

namespace MyProject.Interface
{
    public interface IInventoryService
    {
        Task LogStockChangeAsync(int variantId, int quantityChange, InventoryAction action, string? reason = null, int? invoiceId = null, int? userId = null);
        Task<IEnumerable<InventoryLog>> GetLogsByVariantIdAsync(int variantId);
        Task<IEnumerable<InventoryLog>> GetAllLogsAsync();
    }
}
