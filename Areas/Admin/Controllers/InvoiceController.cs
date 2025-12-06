using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyProject.Interface;
using MyProject.Areas.User.Models;
using MyProject.Areas.Admin.Models;
using MyProject.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MyProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IInvoiceDetailService _invoiceDetailService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IOrderAuditService _orderAuditService;

        public InvoiceController(
            IInvoiceService invoiceService,
            IInvoiceDetailService invoiceDetailService,
            IUserService userService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IOrderAuditService orderAuditService)
        {
            _invoiceService = invoiceService;
            _invoiceDetailService = invoiceDetailService;
            _userService = userService;
            _userManager = userManager;
            _context = context;
            _orderAuditService = orderAuditService;
        }

        // Admin/Staff view all invoices
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Index(OrderStatus? status = null)
        {
            var query = _context.Invoices
                .Include(i => i.User)
                .Include(i => i.InvoiceDetails!)
                    .ThenInclude(id => id.Variant)
                .Where(i => !i.IsDeleted)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(i => i.Status == status.Value);
                ViewBag.Status = status;
            }

            query = query.OrderByDescending(i => i.CreatedAt);
            var invoices = await query.ToListAsync();

            return View(invoices);
        }

        // View invoice details
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            // Check authorization
            var userId = await GetCurrentUserIdAsync();
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
            
            if (!isAdmin && invoice.UserId != userId)
            {
                return Forbid();
            }

            var invoiceDetails = await _invoiceDetailService.GetByInvoiceIdAsync(id);
            ViewBag.InvoiceDetails = invoiceDetails;
            ViewBag.TotalAmount = await _invoiceDetailService.GetTotalAmountByInvoiceIdAsync(id);
            ViewBag.AuditLogs = await _orderAuditService.GetLogsByInvoiceIdAsync(id);

            return View(invoice);
        }

        // Customer view their orders
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var invoices = await _invoiceService.GetByUserIdAsync(userId.Value);
            return View(invoices);
        }

        // Admin/Staff confirm order
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Confirm(int invoiceId)
        {
            try
            {
                var success = await _invoiceService.ConfirmAsync(invoiceId);
                var message = success ? "Đã xác nhận đơn hàng" : "Không thể xác nhận đơn hàng";
                return Json(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Admin/Staff mark as shipped
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> MarkShipped(int invoiceId)
        {
            try
            {
                var success = await _invoiceService.MarkShippedAsync(invoiceId);
                var message = success ? "Đã chuyển trạng thái shipped" : "Không thể chuyển trạng thái shipped";
                return Json(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Customer complete order
        [HttpPost]
        public async Task<IActionResult> CompleteOrder(int invoiceId)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId == null)
                {
                    return Json(new { success = false, message = "Bạn cần đăng nhập!" });
                }

                var success = await _invoiceService.MarkCompletedByUserAsync(invoiceId, userId.Value);
                var message = success ? "Cảm ơn bạn! Đơn hàng đã hoàn thành." : "Không thể xác nhận đã nhận hàng.";

                return Json(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Customer request cancel
        [HttpPost]
        public async Task<IActionResult> RequestCancel(int invoiceId, string? reason)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId == null)
                {
                    return Json(new { success = false, message = "Bạn cần đăng nhập!" });
                }

                var success = await _invoiceService.RequestCancelAsync(invoiceId, userId.Value, reason);
                var message = success ? "Đã gửi yêu cầu hủy. Vui lòng chờ duyệt." : "Không thể gửi yêu cầu hủy.";

                return Json(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Admin approve cancel request
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveCancel(int invoiceId, string? reason)
        {
            try
            {
                var success = await _invoiceService.ApproveCancelAsync(invoiceId, reason);
                var message = success ? "Đã chấp nhận hủy đơn." : "Không thể chấp nhận hủy.";

                return Json(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Admin reject cancel request
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectCancel(int invoiceId)
        {
            try
            {
                var success = await _invoiceService.RejectCancelAsync(invoiceId);
                var message = success ? "Đã từ chối yêu cầu hủy." : "Không thể từ chối yêu cầu hủy.";

                return Json(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Admin view cancel requests
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> CancelRequests()
        {
            try
            {
                var cancelRequests = await _invoiceService.GetCancelRequestsAsync();
                return View(cancelRequests);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Không thể tải danh sách yêu cầu hủy: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // Admin cancel order
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminCancel(int invoiceId, string? reason)
        {
            try
            {
                var success = await _invoiceService.AdminCancelAsync(invoiceId, reason);
                var message = success ? "Đã hủy đơn hàng" : "Không thể hủy đơn hàng";

                return Json(new { success = success, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Helper method
        private async Task<int?> GetCurrentUserIdAsync()
        {
            if (!User.Identity?.IsAuthenticated == true)
                return null;

            var identityUser = await _userManager.GetUserAsync(User);
            return identityUser?.DomainUserId;
        }
    }
}
