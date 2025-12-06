using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Areas.Admin.Models;
using MyProject.Interface;
using MyProject.Models.Shared;

namespace MyProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly IVariantService _variantService;

        public InventoryController(IInventoryService inventoryService, IVariantService variantService)
        {
            _inventoryService = inventoryService;
            _variantService = variantService;
        }

        public async Task<IActionResult> Index(int? variantId)
        {
            IEnumerable<InventoryLog> logs;
            if (variantId.HasValue)
            {
                logs = await _inventoryService.GetLogsByVariantIdAsync(variantId.Value);
                ViewBag.CurrentVariantId = variantId;
            }
            else
            {
                logs = await _inventoryService.GetAllLogsAsync();
            }

            return View(logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(int variantId, int quantityChange, string reason)
        {
            if (quantityChange == 0)
            {
                TempData["Error"] = "Quantity change must be non-zero.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var action = quantityChange > 0 ? InventoryAction.Import : InventoryAction.Adjust;
                // Get current user ID if possible
                // int? userId = ...; 
                
                await _inventoryService.LogStockChangeAsync(variantId, quantityChange, action, reason, null, null);
                TempData["Success"] = "Stock adjusted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error adjusting stock: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
