using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyProject.Data;
using MyProject.Models.Shared;

namespace MyProject.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index(string? search, int? categoryId, string? sortBy, decimal? minPrice, decimal? maxPrice, int page = 1)
        {
            const int pageSize = 12;

            // Base query with includes
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Include(p => p.Reviews)
                .Where(p => p.Variants.Any(v => v.Quanlity > 0)) // Only show products with stock
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.ProductName.Contains(search) || 
                                        (p.Description != null && p.Description.Contains(search)));
            }

            // Category filter
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Price filter
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Variants.Any(v => v.Price >= minPrice.Value));
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Variants.Any(v => v.Price <= maxPrice.Value));
            }

            // Sorting
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Variants.Min(v => v.Price)),
                "price_desc" => query.OrderByDescending(p => p.Variants.Min(v => v.Price)),
                "name_asc" => query.OrderBy(p => p.ProductName),
                "name_desc" => query.OrderByDescending(p => p.ProductName),
                "newest" => query.OrderByDescending(p => p.ProductId),
                _ => query.OrderBy(p => p.ProductName)
            };

            // Calculate statistics for each product
            var products = await query.ToListAsync();
            
            foreach (var product in products)
            {
                if (product.Reviews != null && product.Reviews.Any())
                {
                    product.AverageRating = product.Reviews.Average(r => r.Rating);
                    product.ReviewCount = product.Reviews.Count;
                }
            }

            // Pagination
            var totalItems = products.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var paginatedProducts = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // ViewBag data
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentSort = sortBy;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(paginatedProducts);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeType)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Calculate review statistics
            if (product.Reviews != null && product.Reviews.Any())
            {
                product.AverageRating = product.Reviews.Average(r => r.Rating);
                product.ReviewCount = product.Reviews.Count;
            }

            // Get related products
            var relatedProducts = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Include(p => p.Reviews)
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
                .Where(p => p.Variants.Any(v => v.Quanlity > 0))
                .Take(4)
                .ToListAsync();

            foreach (var relatedProduct in relatedProducts)
            {
                if (relatedProduct.Reviews != null && relatedProduct.Reviews.Any())
                {
                    relatedProduct.AverageRating = relatedProduct.Reviews.Average(r => r.Rating);
                    relatedProduct.ReviewCount = relatedProduct.Reviews.Count;
                }
            }

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }
    }
}
