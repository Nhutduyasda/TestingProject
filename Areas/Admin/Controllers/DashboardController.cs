using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyProject.Data;
using MyProject.Areas.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace MyProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Dashboard home
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dashboardData = await GetDashboardDataAsync();
            return View(dashboardData);
        }

        // Get analytics data for charts
        [HttpGet]
        public async Task<JsonResult> GetAnalyticsData()
        {
            var data = await GetDashboardDataAsync();
            return Json(data);
        }

        // Sales chart data
        [HttpGet]
        public async Task<JsonResult> GetSalesChart(int months = 6)
        {
            try
            {
                var endDate = DateTime.Now;
                var startDate = endDate.AddMonths(-months);

                var salesData = await _context.Invoices
                    .Where(i => i.CreatedAt >= startDate && i.CreatedAt <= endDate && !i.IsDeleted)
                    .GroupBy(i => new { i.CreatedAt.Year, i.CreatedAt.Month })
                    .Select(g => new
                    {
                        Month = $"T{g.Key.Month}/{g.Key.Year}",
                        Sales = g.Sum(i => i.TotalAmount),
                        Orders = g.Count()
                    })
                    .OrderBy(x => x.Month)
                    .ToListAsync();

                return Json(salesData);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Products by category chart
        [HttpGet]
        public async Task<JsonResult> GetProductsChart()
        {
            try
            {
                var categoryData = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.Category != null)
                    .GroupBy(p => p.Category!.CategoryName)
                    .Select(g => new
                    {
                        Category = g.Key ?? "Chưa phân loại",
                        Count = g.Count()
                    })
                    .ToListAsync();

                return Json(categoryData);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Top selling products
        [HttpGet]
        public async Task<JsonResult> GetTopProducts(int count = 10)
        {
            try
            {
                var topProducts = await _context.InvoiceDetails
                    .Include(id => id.Variant)
                        .ThenInclude(v => v!.Product)
                    .Include(id => id.Invoice)
                    .Where(id => id.Variant != null && id.Variant.Product != null && id.Invoice != null && !id.Invoice.IsDeleted)
                    .GroupBy(id => new
                    {
                        id.Variant!.Product!.ProductId,
                        id.Variant.Product.ProductName
                    })
                    .Select(g => new
                    {
                        ProductName = g.Key.ProductName ?? "Sản phẩm",
                        TotalSold = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                    })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(count)
                    .ToListAsync();

                return Json(topProducts);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Low stock products
        [HttpGet]
        public async Task<JsonResult> GetLowStockProducts()
        {
            try
            {
                var lowStockProducts = await _context.Variants
                    .Include(v => v.Product)
                        .ThenInclude(p => p!.Category)
                    .Where(v => v.Quanlity <= 10)
                    .Select(v => new
                    {
                        VariantId = v.VariantId,
                        ProductName = v.Product != null ? v.Product.ProductName : "Unknown",
                        VariantName = v.VariantName,
                        Stock = v.Quanlity,
                        Category = v.Product != null && v.Product.Category != null ? v.Product.Category.CategoryName : "Chưa phân loại"
                    })
                    .OrderBy(v => v.Stock)
                    .ToListAsync();

                return Json(lowStockProducts);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Recent orders
        [HttpGet]
        public async Task<JsonResult> GetRecentOrders(int count = 10)
        {
            try
            {
                var recentOrders = await _context.Invoices
                    .Include(i => i.User)
                    .Where(i => !i.IsDeleted)
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(count)
                    .Select(i => new
                    {
                        i.InvoiceId,
                        CustomerName = i.User != null ? $"{i.User.FirstMidName} {i.User.LastName}" : "Khách vãng lai",
                        i.TotalAmount,
                        OrderStatus = i.Status.ToDisplayText(),
                        CreateDate = i.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                    })
                    .ToListAsync();

                return Json(recentOrders);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Get dashboard statistics
        private async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            try
            {
                var products = await _context.Products.Include(p => p.Variants).ToListAsync();
                var invoices = await _context.Invoices.Where(i => !i.IsDeleted).ToListAsync();
                var users = await _context.Users.ToListAsync();
                var categories = await _context.Categories.ToListAsync();
                var suppliers = await _context.Suppliers.ToListAsync();

                // Calculate this month's data
                var thisMonth = DateTime.Now;
                var firstDayOfMonth = new DateTime(thisMonth.Year, thisMonth.Month, 1);
                var lastMonth = firstDayOfMonth.AddMonths(-1);

                var thisMonthInvoices = invoices.Where(i => i.CreatedAt >= firstDayOfMonth).ToList();
                var lastMonthInvoices = invoices.Where(i => i.CreatedAt >= lastMonth && i.CreatedAt < firstDayOfMonth).ToList();

                var thisMonthRevenue = thisMonthInvoices.Sum(i => i.TotalAmount);
                var lastMonthRevenue = lastMonthInvoices.Sum(i => i.TotalAmount);

                var revenueGrowth = lastMonthRevenue > 0
                    ? ((thisMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100
                    : 0;

                // Calculate stock totals
                var allVariants = await _context.Variants.ToListAsync();
                var lowStockCount = allVariants.Count(v => v.Quanlity <= 10);
                var outOfStockCount = allVariants.Count(v => v.Quanlity == 0);
                var totalStock = allVariants.Sum(v => v.Quanlity);

                return new DashboardViewModel
                {
                    TotalProducts = products.Count,
                    TotalCategories = categories.Count,
                    TotalSuppliers = suppliers.Count,
                    TotalUsers = users.Count,

                    TotalOrders = invoices.Count,
                    PendingOrders = invoices.Count(i => i.Status == OrderStatus.Pending),
                    CompletedOrders = invoices.Count(i => i.Status == OrderStatus.Completed),
                    CancelledOrders = invoices.Count(i => i.Status == OrderStatus.Cancelled),

                    TotalRevenue = invoices.Sum(i => i.TotalAmount),
                    ThisMonthRevenue = thisMonthRevenue,
                    RevenueGrowthPercent = (decimal)revenueGrowth,

                    LowStockProducts = lowStockCount,
                    OutOfStockProducts = outOfStockCount,
                    TotalStock = totalStock,

                    AverageOrderValue = invoices.Any() ? invoices.Average(i => i.TotalAmount) : 0,

                    RecentOrdersCount = thisMonthInvoices.Count,
                    NewCustomersThisMonth = users.Count(u => u.RegistrationDate >= firstDayOfMonth)
                };
            }
            catch (Exception)
            {
                return new DashboardViewModel();
            }
        }
    }

    // Dashboard ViewModel
    public class DashboardViewModel
    {
        // Product Statistics
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalSuppliers { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int TotalStock { get; set; }

        // Order Statistics
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int RecentOrdersCount { get; set; }

        // Revenue Statistics
        public decimal TotalRevenue { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public decimal RevenueGrowthPercent { get; set; }
        public decimal AverageOrderValue { get; set; }

        // User Statistics
        public int TotalUsers { get; set; }
        public int NewCustomersThisMonth { get; set; }
    }
}
