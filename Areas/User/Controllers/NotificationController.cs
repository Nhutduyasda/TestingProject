using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyProject.Data;
using MyProject.Interface;

namespace MyProject.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.DomainUserId == null) return Challenge();

            var notifications = await _notificationService.GetUserNotificationsAsync(user.DomainUserId.Value);
            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.DomainUserId == null) return Json(new { success = false });

            await _notificationService.MarkAllAsReadAsync(user.DomainUserId.Value);
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.DomainUserId == null) return Json(new { count = 0 });

            var count = await _notificationService.GetUnreadCountAsync(user.DomainUserId.Value);
            return Json(new { count = count });
        }
    }
}
