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
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IInvoiceService _invoiceService;

        public OrdersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IInvoiceService invoiceService)
        {
            _context = context;
            _userManager = userManager;
            _invoiceService = invoiceService;
        }

        public async Task<IActionResult> Index(string? status)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem đơn hàng!";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var query = _context.Invoices
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(id => id.Variant)
                        .ThenInclude(v => v!.Product)
                            .ThenInclude(p => p!.Images)
                .Where(i => i.UserId == userId.Value);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
            {
                query = query.Where(i => i.Status == orderStatus);
                ViewBag.CurrentStatus = status;
            }

            var orders = await query
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem chi tiết đơn hàng!";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(id => id.Variant)
                        .ThenInclude(v => v!.Product)
                            .ThenInclude(p => p!.Images)
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.InvoiceId == id && i.UserId == userId.Value);

            if (invoice == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Index");
            }

            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCancel(int id, string? reason)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập!" });
            }

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.InvoiceId == id && i.UserId == userId.Value);

            if (invoice == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            if (invoice.Status != OrderStatus.Pending)
            {
                return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng đang chờ xác nhận!" });
            }

            try
            {
                invoice.Status = OrderStatus.CancelRequested;
                invoice.CancelReason = reason ?? "Khách hàng yêu cầu hủy";
                invoice.CancelledAt = DateTime.Now;
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã gửi yêu cầu hủy đơn hàng!";
                return Json(new { success = true, message = "Đã gửi yêu cầu hủy đơn hàng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReOrder(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập!" });
            }

            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(id => id.Variant)
                .FirstOrDefaultAsync(i => i.InvoiceId == id && i.UserId == userId.Value);

            if (invoice == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
            }

            try
            {
                // Get or create cart
                var cart = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);

                if (cart == null)
                {
                    cart = new MyProject.Areas.User.Models.Cart
                    {
                        UserId = userId.Value,
                        CreatedAt = DateTime.Now
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                // Add items to cart
                foreach (var item in invoice.InvoiceDetails)
                {
                    var existingCartDetail = await _context.CartDetails
                        .FirstOrDefaultAsync(cd => cd.CartId == cart.CartId && cd.VariantId == item.VariantId);

                    if (existingCartDetail != null)
                    {
                        existingCartDetail.Quantity += item.Quantity;
                    }
                    else
                    {
                        if (item.VariantId.HasValue)
                        {
                            _context.CartDetails.Add(new MyProject.Areas.User.Models.CartDetail
                            {
                                CartId = cart.CartId,
                                VariantId = item.VariantId.Value,
                                Quantity = item.Quantity
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm các sản phẩm vào giỏ hàng!";
                return Json(new { success = true, message = "Đã thêm vào giỏ hàng!", redirectUrl = Url.Action("MyCart", "Cart", new { area = "" }) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
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
