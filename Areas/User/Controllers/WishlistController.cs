using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyProject.Data;
using MyProject.Interface;

namespace MyProject.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlistService;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(IWishlistService wishlistService, UserManager<ApplicationUser> userManager)
        {
            _wishlistService = wishlistService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.DomainUserId == null) return Challenge();

            var items = await _wishlistService.GetByUserIdAsync(user.DomainUserId.Value);
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.DomainUserId == null) return Json(new { success = false, message = "Bạn cần đăng nhập!" });

            try
            {
                if (await _wishlistService.IsInWishlistAsync(user.DomainUserId.Value, productId))
                {
                    await _wishlistService.RemoveAsync(user.DomainUserId.Value, productId);
                    return Json(new { success = true, isAdded = false, message = "Đã xóa khỏi danh sách yêu thích" });
                }
                else
                {
                    await _wishlistService.AddAsync(user.DomainUserId.Value, productId);
                    return Json(new { success = true, isAdded = true, message = "Đã thêm vào danh sách yêu thích" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}
