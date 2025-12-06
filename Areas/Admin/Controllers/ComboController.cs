using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyProject.Data;
using MyProject.Models.Shared;

namespace MyProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ComboController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ComboController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var combos = await _context.Combos
                .Include(c => c.Category)
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
            return View(combos);
        }

        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            
            var products = _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .ToList() 
                .Select(p => new {
                    p.ProductId,
                    Name = p.ProductName,
                    Price = p.Variants.Any() ? p.Variants.Min(v => v.Price) : 0,
                    ImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? p.Images.FirstOrDefault()?.ImageUrl ?? "/images/default-product.png",
                    Variants = p.Variants.Select(v => new {
                        v.VariantId,
                        v.VariantName,
                        v.Price,
                        v.Quanlity
                    }).ToList()
                }).ToList();

            ViewBag.Products = products;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Combo combo, int[] variantIds, int[] quantities, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                // Handle Image Upload
                if (file != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\combos");

                    if (!Directory.Exists(productPath))
                    {
                        Directory.CreateDirectory(productPath);
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    combo.ImageUrl = @"\images\combos\" + fileName;
                }

                // Calculate Original Price
                decimal originalPrice = 0;
                if (variantIds != null && quantities != null && variantIds.Length == quantities.Length)
                {
                    for (int i = 0; i < variantIds.Length; i++)
                    {
                        var variant = await _context.Variants
                            .Include(v => v.Product)
                            .FirstOrDefaultAsync(v => v.VariantId == variantIds[i]);
                            
                        if (variant != null)
                        {
                            var comboProduct = new ComboProduct
                            {
                                ProductId = variant.ProductId,
                                VariantId = variant.VariantId,
                                Quantity = quantities[i],
                                UnitPrice = variant.Price,
                                Combo = combo
                            };
                            combo.ComboProducts.Add(comboProduct);
                            originalPrice += variant.Price * quantities[i];
                        }
                    }
                }
                combo.OriginalPrice = originalPrice;
                combo.CreatedDate = DateTime.UtcNow;

                _context.Add(combo);
                await _context.SaveChangesAsync();
                TempData["success"] = "Combo created successfully";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "CategoryName", combo.CategoryId);
            
             var productsList = _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .ToList()
                .Select(p => new {
                    p.ProductId,
                    Name = p.ProductName,
                    Price = p.Variants.Any() ? p.Variants.Min(v => v.Price) : 0,
                    ImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? p.Images.FirstOrDefault()?.ImageUrl ?? "/images/default-product.png",
                    Variants = p.Variants.Select(v => new {
                        v.VariantId,
                        v.VariantName,
                        v.Price,
                        v.Quanlity
                    }).ToList()
                }).ToList();
            ViewBag.Products = productsList;
            
            return View(combo);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var combo = await _context.Combos
                .Include(c => c.ComboProducts)
                .ThenInclude(cp => cp.Product)
                .Include(c => c.ComboProducts)
                .ThenInclude(cp => cp.Variant)
                .FirstOrDefaultAsync(c => c.ComboId == id);

            if (combo == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "CategoryName", combo.CategoryId);
            
            var products = _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .ToList()
                .Select(p => new {
                    p.ProductId,
                    Name = p.ProductName,
                    Price = p.Variants.Any() ? p.Variants.Min(v => v.Price) : 0,
                    ImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? p.Images.FirstOrDefault()?.ImageUrl ?? "/images/default-product.png",
                    Variants = p.Variants.Select(v => new {
                        v.VariantId,
                        v.VariantName,
                        v.Price,
                        v.Quanlity
                    }).ToList()
                }).ToList();
            ViewBag.Products = products;

            return View(combo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Combo combo, int[] variantIds, int[] quantities, IFormFile? file)
        {
            if (id != combo.ComboId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCombo = await _context.Combos
                        .Include(c => c.ComboProducts)
                        .FirstOrDefaultAsync(c => c.ComboId == id);

                    if (existingCombo == null)
                    {
                        return NotFound();
                    }

                    // Update basic fields
                    existingCombo.ComboName = combo.ComboName;
                    existingCombo.Description = combo.Description;
                    existingCombo.Price = combo.Price;
                    existingCombo.CategoryId = combo.CategoryId;
                    existingCombo.IsActive = combo.IsActive;
                    existingCombo.AvailableQuantity = combo.AvailableQuantity;
                    existingCombo.ModifiedDate = DateTime.UtcNow;

                    // Handle Image Upload
                    if (file != null)
                    {
                        string wwwRootPath = _webHostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = Path.Combine(wwwRootPath, @"images\combos");

                        if (!Directory.Exists(productPath))
                        {
                            Directory.CreateDirectory(productPath);
                        }

                        // Delete old image
                        if (!string.IsNullOrEmpty(existingCombo.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(wwwRootPath, existingCombo.ImageUrl.TrimStart('\\'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        existingCombo.ImageUrl = @"\images\combos\" + fileName;
                    }

                    // Update Products
                    // Remove existing products
                    _context.ComboProducts.RemoveRange(existingCombo.ComboProducts);
                    
                    // Add new products
                    decimal originalPrice = 0;
                    if (variantIds != null && quantities != null && variantIds.Length == quantities.Length)
                    {
                        for (int i = 0; i < variantIds.Length; i++)
                        {
                            var variant = await _context.Variants
                                .Include(v => v.Product)
                                .FirstOrDefaultAsync(v => v.VariantId == variantIds[i]);
                                
                            if (variant != null)
                            {
                                var comboProduct = new ComboProduct
                                {
                                    ProductId = variant.ProductId,
                                    VariantId = variant.VariantId,
                                    Quantity = quantities[i],
                                    UnitPrice = variant.Price,
                                    ComboId = existingCombo.ComboId
                                };
                                _context.ComboProducts.Add(comboProduct);
                                originalPrice += variant.Price * quantities[i];
                            }
                        }
                    }
                    existingCombo.OriginalPrice = originalPrice;

                    _context.Update(existingCombo);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Combo updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ComboExists(combo.ComboId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "CategoryName", combo.CategoryId);
            
            var productsList = _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .ToList()
                .Select(p => new {
                    p.ProductId,
                    Name = p.ProductName,
                    Price = p.Variants.Any() ? p.Variants.Min(v => v.Price) : 0,
                    ImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? p.Images.FirstOrDefault()?.ImageUrl ?? "/images/default-product.png",
                    Variants = p.Variants.Select(v => new {
                        v.VariantId,
                        v.VariantName,
                        v.Price,
                        v.Quanlity
                    }).ToList()
                }).ToList();
            ViewBag.Products = productsList;
            
            return View(combo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var combo = await _context.Combos.FindAsync(id);
            if (combo != null)
            {
                combo.IsDeleted = true; // Soft delete
                // combo.IsActive = false; // Optionally deactivate
                await _context.SaveChangesAsync();
                TempData["success"] = "Combo deleted successfully";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ComboExists(int id)
        {
            return _context.Combos.Any(e => e.ComboId == id);
        }
    }
}
