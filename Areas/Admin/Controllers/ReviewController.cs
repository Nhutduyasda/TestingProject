using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Data;
using MyProject.Interface;
using Microsoft.EntityFrameworkCore;

namespace MyProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly ApplicationDbContext _context;

        public ReviewController(IReviewService reviewService, ApplicationDbContext context)
        {
            _reviewService = reviewService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var reviews = await _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _reviewService.DeleteReviewAsync(id);
            TempData["Success"] = "Đã xóa đánh giá.";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleApproval(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);
            if (review != null)
            {
                review.IsApproved = !review.IsApproved;
                await _context.SaveChangesAsync();
                TempData["Success"] = review.IsApproved ? "Đã duyệt đánh giá." : "Đã ẩn đánh giá.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
