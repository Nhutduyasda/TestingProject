using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyProject.Data;
using MyProject.Models;

namespace MyProject.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Get featured products (products with stock)
        var featuredProducts = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Reviews)
            .Where(p => p.Variants.Any(v => v.Quanlity > 0))
            .OrderByDescending(p => p.ProductId)
            .Take(4)
            .ToListAsync();

        // Calculate review statistics for each product
        foreach (var product in featuredProducts)
        {
            if (product.Reviews != null && product.Reviews.Any())
            {
                product.AverageRating = product.Reviews.Average(r => r.Rating);
                product.ReviewCount = product.Reviews.Count;
            }
        }

        return View(featuredProducts);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
