using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyProject.Models.Shared;
using MyProject.Data;
using Microsoft.EntityFrameworkCore;
using MyProject.Service;

namespace MyProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public ProductController(ApplicationDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        // Admin product listing
        [HttpGet]
        public async Task<IActionResult> Index(int? categoryId, string? search)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
                ViewBag.CategoryId = categoryId;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.ProductName.Contains(search) || 
                                        (p.Description != null && p.Description.Contains(search)));
                ViewBag.Search = search;
            }

            var products = await query.ToListAsync();
            
            // Load categories for filter
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(products);
        }

        // Product details
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeType)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // Create product
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Suppliers = await _context.Suppliers.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, List<IFormFile>? productImages)
        {
            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                await ProcessProductImages(product.ProductId, productImages, product.ProductName);

                TempData["SuccessMessage"] = "Tạo sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Suppliers = await _context.Suppliers.ToListAsync();
            return View(product);
        }

        // Edit product
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductId == id);
                
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Suppliers = await _context.Suppliers.ToListAsync();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, List<IFormFile>? productImages)
        {
            if (id != product.ProductId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    await ProcessProductImages(product.ProductId, productImages, product.ProductName);

                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ProductExists(product.ProductId)) return NotFound();
                    else throw;
                }
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Suppliers = await _context.Suppliers.ToListAsync();
            return View(product);
        }

        private async Task ProcessProductImages(int productId, List<IFormFile>? productImages, string productName)
        {
            if (productImages == null || !productImages.Any()) return;

            var maxOrder = await _context.ProductImages
                .Where(pi => pi.ProductId == productId)
                .MaxAsync(pi => (int?)pi.DisplayOrder) ?? 0;

            int displayOrder = maxOrder + 1;

            foreach (var image in productImages)
            {
                try
                {
                    var imageUrl = await _cloudinaryService.UploadImageAsync(image, "products");
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        var productImage = new ProductImage
                        {
                            ProductId = productId,
                            ImageUrl = imageUrl,
                            IsPrimary = displayOrder == 1,
                            DisplayOrder = displayOrder++,
                            AltText = productName
                        };
                        _context.ProductImages.Add(productImage);
                    }
                }
                catch (Exception ex)
                {
                    TempData["WarningMessage"] = $"Một số ảnh không thể upload: {ex.Message}";
                }
            }
            await _context.SaveChangesAsync();
        }

        // Delete product
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ProductExists(int id)
        {
            return await _context.Products.AnyAsync(e => e.ProductId == id);
        }
    }
}
