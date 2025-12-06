using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyProject.Areas.User.Models;
using MyProject.Data;
using MyProject.Interface;

namespace MyProject.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(IReviewService reviewService, UserManager<ApplicationUser> userManager)
        {
            _reviewService = reviewService;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(ProductReview review)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction("Details", "Products", new { area = "", id = review.ProductId });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!await _reviewService.CanUserReviewAsync(user.DomainUserId.Value, review.ProductId))
            {
                TempData["Error"] = "Bạn không thể đánh giá sản phẩm này (chưa mua hoặc đã đánh giá).";
                return RedirectToAction("Details", "Products", new { area = "", id = review.ProductId });
            }

            review.UserId = user.DomainUserId.Value;
            review.CreatedAt = DateTime.UtcNow;
            review.IsApproved = true; // Auto-approve for now, or set to false if moderation is needed

            await _reviewService.AddReviewAsync(review);
            TempData["Success"] = "Đánh giá của bạn đã được gửi.";

            return RedirectToAction("Details", "Products", new { area = "", id = review.ProductId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProductReview review)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction("Details", "Products", new { area = "", id = review.ProductId });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var existingReview = await _reviewService.GetUserReviewAsync(user.DomainUserId.Value, review.ProductId);
            if (existingReview == null)
            {
                TempData["Error"] = "Không tìm thấy đánh giá.";
                return RedirectToAction("Details", "Products", new { area = "", id = review.ProductId });
            }

            if (existingReview.UserId != user.DomainUserId.Value)
            {
                TempData["Error"] = "Bạn không có quyền sửa đánh giá này.";
                return RedirectToAction("Details", "Products", new { area = "", id = review.ProductId });
            }

            review.ReviewId = existingReview.ReviewId;
            await _reviewService.UpdateReviewAsync(review);
            TempData["Success"] = "Đánh giá của bạn đã được cập nhật.";

            return RedirectToAction("Details", "Products", new { area = "", id = review.ProductId });
        }
    }
}
