using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyProject.Models.Shared;
using MyProject.Data;
using Microsoft.EntityFrameworkCore;

namespace MyProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupplierController(ApplicationDbContext context)
        {
            _context = context;
        }

        // List all suppliers
        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm)
        {
            var query = _context.Suppliers.AsQueryable();

            // Search by name, email, or phone
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(s => 
                    (s.SupplierName != null && s.SupplierName.ToLower().Contains(searchTerm)) ||
                    (s.Email != null && s.Email.ToLower().Contains(searchTerm)) ||
                    (s.Phone != null && s.Phone.Contains(searchTerm)));
            }

            var suppliers = await query
                .Include(s => s.Products)
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

            ViewData["SearchTerm"] = searchTerm;
            return View(suppliers);
        }

        // Supplier details
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.Products)
                    .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(s => s.SupplierId == id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // Create supplier
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate email
                if (!string.IsNullOrWhiteSpace(supplier.Email))
                {
                    var existingSupplier = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.Email == supplier.Email);
                    
                    if (existingSupplier != null)
                    {
                        ModelState.AddModelError("Email", "Email đã được sử dụng bởi nhà cung cấp khác.");
                        return View(supplier);
                    }
                }

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tạo nhà cung cấp thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // Edit supplier
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.SupplierId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate email (excluding current supplier)
                    if (!string.IsNullOrWhiteSpace(supplier.Email))
                    {
                        var existingSupplier = await _context.Suppliers
                            .FirstOrDefaultAsync(s => s.Email == supplier.Email && s.SupplierId != id);
                        
                        if (existingSupplier != null)
                        {
                            ModelState.AddModelError("Email", "Email đã được sử dụng bởi nhà cung cấp khác.");
                            return View(supplier);
                        }
                    }

                    _context.Update(supplier);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật nhà cung cấp thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await SupplierExists(supplier.SupplierId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(supplier);
        }

        // Delete supplier
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.SupplierId == id);

            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.SupplierId == id);

            if (supplier != null)
            {
                // Check if supplier has products
                if (supplier.Products?.Any() == true)
                {
                    TempData["ErrorMessage"] = "Không thể xóa nhà cung cấp đang có sản phẩm! Vui lòng xóa hoặc chuyển sản phẩm sang nhà cung cấp khác trước.";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }

                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa nhà cung cấp thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // Get products by supplier (API endpoint for AJAX calls)
        [HttpGet]
        public async Task<JsonResult> GetProductsBySupplier(int supplierId)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.SupplierId == supplierId)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        CategoryName = p.Category != null ? p.Category.CategoryName : "Chưa phân loại"
                    })
                    .ToListAsync();

                return Json(products);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Get supplier statistics (API endpoint)
        [HttpGet]
        public async Task<JsonResult> GetSupplierStats(int supplierId)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.Products)
                        .ThenInclude(p => p.Variants)
                    .FirstOrDefaultAsync(s => s.SupplierId == supplierId);

                if (supplier == null)
                {
                    return Json(new { error = "Supplier not found" });
                }

                var totalProducts = supplier.Products?.Count ?? 0;
                var totalVariants = supplier.Products?.SelectMany(p => p.Variants ?? new List<Variant>()).Count() ?? 0;
                var totalStock = supplier.Products?
                    .SelectMany(p => p.Variants ?? new List<Variant>())
                    .Sum(v => v.Quanlity) ?? 0;

                var stats = new
                {
                    TotalProducts = totalProducts,
                    TotalVariants = totalVariants,
                    TotalStock = totalStock,
                    LowStockVariants = supplier.Products?
                        .SelectMany(p => p.Variants ?? new List<Variant>())
                        .Count(v => v.Quanlity <= 10) ?? 0
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        private async Task<bool> SupplierExists(int id)
        {
            return await _context.Suppliers.AnyAsync(e => e.SupplierId == id);
        }
    }
}
