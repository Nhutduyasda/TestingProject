using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyProject.Interface;
using MyProject.Models.Shared;
using MyProject.Data;
using Microsoft.EntityFrameworkCore;

namespace MyProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class VariantController : Controller
    {
        private readonly IVariantService _variantService;
        private readonly ApplicationDbContext _context;

        public VariantController(IVariantService variantService, ApplicationDbContext context)
        {
            _variantService = variantService;
            _context = context;
        }

        // List variants for a product (or all variants if no productId)
        [HttpGet]
        public async Task<IActionResult> Index(int? productId)
        {
            IEnumerable<Variant> variants;
            
            if (productId.HasValue)
            {
                var product = await _context.Products.FindAsync(productId.Value);
                if (product == null)
                {
                    return NotFound();
                }
                variants = await _variantService.GetByProductIdAsync(productId.Value);
                ViewBag.Product = product;
            }
            else
            {
                // Show all variants with their products
                variants = await _context.Variants
                    .Include(v => v.Product)
                        .ThenInclude(p => p.Category)
                    .Include(v => v.Product)
                        .ThenInclude(p => p.Images)
                    .Include(v => v.VariantAttributes)
                        .ThenInclude(va => va.AttributeType)
                    .OrderByDescending(v => v.VariantId)
                    .ToListAsync();
            }
            
            return View(variants);
        }

        // Variant details
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var variant = await _context.Variants
                .Include(v => v.Product)
                    .ThenInclude(p => p.Category)
                .Include(v => v.Product)
                    .ThenInclude(p => p.Images)
                .Include(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeType)
                .FirstOrDefaultAsync(v => v.VariantId == id);
                
            if (variant == null)
            {
                return NotFound();
            }

            // Prepare attributes dictionary for view
            var attributesDict = variant.VariantAttributes
                .Where(va => va.AttributeType != null)
                .ToDictionary(
                    va => va.AttributeType!.Name,
                    va => va.Value
                );
            ViewBag.VariantAttributes = attributesDict;

            return View(variant);
        }

        // Create variant
        [HttpGet]
        public async Task<IActionResult> Create(int? productId)
        {
            if (!productId.HasValue || productId.Value == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn sản phẩm trước khi tạo biến thể.";
                return RedirectToAction("Index", "Product");
            }
            
            var product = await _context.Products.FindAsync(productId.Value);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Index", "Product");
            }

            ViewBag.Product = product;
            ViewBag.AttributeTypes = await _context.AttributeTypes.ToListAsync();
            
            var variant = new Variant { ProductId = productId.Value };
            return View(variant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Variant variant, Dictionary<int, string>? attributes)
        {
            ModelState.Remove("Product");
            ModelState.Remove("RowVersion");
            if (ModelState.IsValid)
            {
                await _variantService.Add(variant);

                // Add variant attributes if provided
                if (attributes != null && attributes.Any())
                {
                    foreach (var attr in attributes)
                    {
                        if (!string.IsNullOrWhiteSpace(attr.Value))
                        {
                            var variantAttribute = new VariantAttribute
                            {
                                VariantId = variant.VariantId,
                                AttributeTypeId = attr.Key,
                                Value = attr.Value
                            };
                            _context.VariantAttributes.Add(variantAttribute);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Tạo variant thành công!";
                return RedirectToAction(nameof(Index), new { productId = variant.ProductId });
            }

            ViewBag.Product = await _context.Products.FindAsync(variant.ProductId);
            ViewBag.AttributeTypes = await _context.AttributeTypes.ToListAsync();
            return View(variant);
        }

        // Edit variant
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var variant = await _variantService.GetByIdAsync(id);
            if (variant == null)
            {
                return NotFound();
            }

            ViewBag.Product = await _context.Products.FindAsync(variant.ProductId);
            ViewBag.AttributeTypes = await _context.AttributeTypes.ToListAsync();
            
            // Load existing attributes
            var existingAttributes = await _context.VariantAttributes
                .Where(va => va.VariantId == id)
                .ToDictionaryAsync(va => va.AttributeTypeId, va => va.Value);
            ViewBag.ExistingAttributes = existingAttributes;

            return View(variant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Variant variant, Dictionary<int, string>? attributes)
        {
            if (id != variant.VariantId)
            {
                return NotFound();
            }

            ModelState.Remove("Product");
            ModelState.Remove("RowVersion");
            if (ModelState.IsValid)
            {
                try
                {
                    await _variantService.Update(variant);

                    // Update variant attributes
                    if (attributes != null)
                    {
                        // Remove old attributes
                        var oldAttributes = await _context.VariantAttributes
                            .Where(va => va.VariantId == id)
                            .ToListAsync();
                        _context.VariantAttributes.RemoveRange(oldAttributes);

                        // Add new attributes
                        foreach (var attr in attributes)
                        {
                            if (!string.IsNullOrWhiteSpace(attr.Value))
                            {
                                var variantAttribute = new VariantAttribute
                                {
                                    VariantId = variant.VariantId,
                                    AttributeTypeId = attr.Key,
                                    Value = attr.Value
                                };
                                _context.VariantAttributes.Add(variantAttribute);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Cập nhật variant thành công!";
                    return RedirectToAction(nameof(Index), new { productId = variant.ProductId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await VariantExists(variant.VariantId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Product = await _context.Products.FindAsync(variant.ProductId);
            ViewBag.AttributeTypes = await _context.AttributeTypes.ToListAsync();
            return View(variant);
        }

        // Delete variant
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var variant = await _variantService.GetByIdAsync(id);
            if (variant == null)
            {
                return NotFound();
            }

            return View(variant);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var variant = await _variantService.GetByIdAsync(id);
            if (variant != null)
            {
                var productId = variant.ProductId;
                await _variantService.Delete(id);
                TempData["SuccessMessage"] = "Xóa variant thành công!";
                return RedirectToAction(nameof(Index), new { productId = productId });
            }

            return RedirectToAction(nameof(Index));
        }

        // Update stock
        [HttpPost]
        public async Task<IActionResult> UpdateStock(int variantId, int quantity)
        {
            try
            {
                await _variantService.UpdateStockAsync(variantId, quantity);
                return Json(new { success = true, message = "Cập nhật số lượng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        private async Task<bool> VariantExists(int id)
        {
            return await _context.Variants.AnyAsync(e => e.VariantId == id);
        }
    }
}
