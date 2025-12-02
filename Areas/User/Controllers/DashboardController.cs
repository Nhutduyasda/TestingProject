using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyProject.Data;
using MyProject.Interface;
using MyProject.Areas.Admin.Models;

namespace MyProject.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IInvoiceService _invoiceService;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IInvoiceService invoiceService)
        {
            _context = context;
            _userManager = userManager;
            _invoiceService = invoiceService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để truy cập trang này!";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // Get user statistics
            ViewBag.TotalOrders = await _context.Invoices
                .Where(i => i.UserId == userId.Value)
                .CountAsync();

            ViewBag.PendingOrders = await _context.Invoices
                .Where(i => i.UserId == userId.Value && i.Status == OrderStatus.Pending)
                .CountAsync();

            ViewBag.CompletedOrders = await _context.Invoices
                .Where(i => i.UserId == userId.Value && i.Status == OrderStatus.Completed)
                .CountAsync();

            ViewBag.TotalSpent = await _context.Invoices
                .Where(i => i.UserId == userId.Value && i.Status == OrderStatus.Completed)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

            return View();
        }

        private async Task<int?> GetCurrentUserIdAsync()
        {
            if (!User.Identity?.IsAuthenticated == true)
                return null;

            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return null;

            if (identityUser.DomainUserId == null)
            {
                await EnsureUserRecordExists(identityUser);
                identityUser = await _userManager.FindByIdAsync(identityUser.Id);
            }

            return identityUser?.DomainUserId;
        }

        private async Task EnsureUserRecordExists(ApplicationUser? identityUser)
        {
            if (identityUser == null) return;

            if (identityUser.DomainUserId.HasValue)
            {
                var linkedUser = await _context.Users.FindAsync(identityUser.DomainUserId.Value);
                if (linkedUser != null) return;
            }

            var userRecord = new MyProject.Areas.Admin.Models.User
            {
                FirstMidName = identityUser.UserName ?? "User",
                LastName = "",
                Email = identityUser.Email ?? "",
                PhoneNumber = identityUser.PhoneNumber ?? "",
                Address = "Chưa cập nhật",
                PasswordHash = "",
                RegistrationDate = DateTime.Now,
                IsActive = true
            };

            _context.Users.Add(userRecord);
            await _context.SaveChangesAsync();

            identityUser.DomainUserId = userRecord.UserID;
            await _userManager.UpdateAsync(identityUser);
        }
    }
}
