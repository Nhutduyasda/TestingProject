using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyProject.Interface;
using MyProject.Areas.User.Models;
using MyProject.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MyProject.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ICartDetailService _cartDetailService;
        private readonly IVariantService _variantService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public CartController(
            ICartService cartService,
            ICartDetailService cartDetailService,
            IVariantService variantService,
            IUserService userService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _cartService = cartService;
            _cartDetailService = cartDetailService;
            _variantService = variantService;
            _userService = userService;
            _userManager = userManager;
            _context = context;
        }

        // Admin view all carts
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Index()
        {
            var carts = await _cartService.GetAllAsync();
            return View(carts);
        }

        // User view their own cart
        [HttpGet]
        public async Task<IActionResult> MyCart()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem giỏ hàng!";
                return RedirectToAction("Login", "Account");
            }

            var cart = await _cartService.GetCartWithItemsAsync(userId.Value);
            if (cart == null)
            {
                cart = await _cartService.GetOrCreateCartAsync(userId.Value);
            }

            var cartDetails = await _cartDetailService.GetByCartIdAsync(cart.CartId);
            ViewBag.CartDetails = cartDetails;
            ViewBag.TotalPrice = await _cartDetailService.GetTotalPriceByCartIdAsync(cart.CartId);
            ViewBag.TotalItems = await _cartDetailService.GetTotalItemsByCartIdAsync(cart.CartId);

            return View(cart);
        }

        // Add item to cart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int variantId, int quantity = 1)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng!" });
            }

            try
            {
                await _cartService.AddItemAsync(userId.Value, variantId, quantity);
                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng!" });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Add combo to cart
        [HttpPost]
        public async Task<IActionResult> AddComboToCart(int comboId, int quantity = 1)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm combo vào giỏ hàng!" });
            }

            try
            {
                await _cartService.AddItemAsync(userId.Value, null, comboId, quantity);
                return Json(new { success = true, message = "Đã thêm combo vào giỏ hàng!" });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Update quantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartDetailId, int quantity)
        {
            try
            {
                await _cartService.UpdateQuantityAsync(cartDetailId, quantity);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Remove item
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartDetailId)
        {
            try
            {
                await _cartService.RemoveItemAsync(cartDetailId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Clear cart
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện thao tác này!" });
            }

            await _cartService.ClearCartAsync(userId.Value);
            return Json(new { success = true });
        }

        // Checkout page
        [HttpGet]
        public async Task<IActionResult> Checkout(string? selectedIds)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thanh toán!";
                return RedirectToAction("Login", "Account");
            }

            var cart = await _cartService.GetByUserIdAsync(userId.Value);
            if (cart == null)
            {
                return RedirectToAction("MyCart");
            }

            var cartDetails = await _cartDetailService.GetByCartIdAsync(cart.CartId);
            
            // Filter if selectedIds provided
            if (!string.IsNullOrEmpty(selectedIds))
            {
                var selectedIdList = selectedIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                              .Select(id => int.Parse(id))
                                              .ToList();
                cartDetails = cartDetails.Where(cd => selectedIdList.Contains(cd.CartDetailId)).ToList();
            }

            if (!cartDetails.Any())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn sản phẩm để thanh toán!";
                return RedirectToAction("MyCart");
            }

            ViewBag.CartDetails = cartDetails;
            
            // Recalculate totals for selected items
            decimal totalPrice = 0;
            int totalItems = 0;
            foreach (var item in cartDetails)
            {
                var price = item.VariantId.HasValue ? item.Variant?.Price ?? 0 : item.Combo?.Price ?? 0;
                totalPrice += price * item.Quantity;
                totalItems += item.Quantity;
            }

            ViewBag.TotalPrice = totalPrice;
            ViewBag.TotalItems = totalItems;
            ViewBag.SelectedIds = selectedIds;

            return View(cart);
        }

        // Process checkout
        [HttpPost]
        public async Task<IActionResult> ProcessCheckout(string payMethod, string fullName, string phoneNumber, string address, string? note, string? selectedIds)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (userId == null)
                {
                    return Json(new { success = false, message = "Bạn cần đăng nhập để thanh toán!" });
                }

                var cart = await _cartService.GetByUserIdAsync(userId.Value);
                if (cart == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy giỏ hàng!" });
                }

                // Parse selected IDs
                List<int>? selectedIdList = null;
                if (!string.IsNullOrEmpty(selectedIds))
                {
                    selectedIdList = selectedIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                              .Select(id => int.Parse(id))
                                              .ToList();
                }

                // Parse payment method
                if (!Enum.TryParse<PayMethod>(payMethod, true, out PayMethod payMethodEnum))
                {
                    payMethodEnum = PayMethod.Cash;
                }

                // Use InvoiceService to create from cart - this handles validation, stock, and clearing cart
                var invoiceService = HttpContext.RequestServices.GetRequiredService<IInvoiceService>();
                var invoice = await invoiceService.CreateFromCartAsync(cart.CartId, payMethodEnum, fullName, phoneNumber, address, note, selectedIdList);

                return Json(new
                {
                    success = true,
                    message = "Đặt hàng thành công!",
                    invoiceId = invoice.InvoiceId
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the full error here in a real app
                return Json(new { success = false, message = "Có lỗi xảy ra trong quá trình xử lý đơn hàng." });
            }
        }

        // Order success page
        [HttpGet]
        public IActionResult OrderSuccess(int? invoiceId)
        {
            ViewBag.InvoiceId = invoiceId;
            return View();
        }

        // Helper method to get current user's domain ID
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

        // Ensure user record exists in domain
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
                PasswordHash = "", // Not used, Identity manages passwords
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
