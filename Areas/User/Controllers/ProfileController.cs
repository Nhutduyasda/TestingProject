using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyProject.Data;

namespace MyProject.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để truy cập trang này!";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng!";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            string firstName,
            string lastName,
            string phoneNumber,
            string address,
            DateTime? dateOfBirth)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập!" });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin người dùng!" });
            }

            try
            {
                user.FirstMidName = firstName;
                user.LastName = lastName;
                user.PhoneNumber = phoneNumber;
                user.Address = address;
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return Json(new { success = true, message = "Cập nhật thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            string currentPassword,
            string newPassword,
            string confirmPassword)
        {
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            {
                return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin!" });
            }

            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "Mật khẩu xác nhận không khớp!" });
            }

            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });
            }

            var result = await _userManager.ChangePasswordAsync(identityUser, currentPassword, newPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = errors });
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
