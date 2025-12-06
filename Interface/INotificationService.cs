using MyProject.Areas.User.Models;

namespace MyProject.Interface
{
    public interface INotificationService
    {
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
        Task CreateNotificationAsync(int userId, string title, string message, string? type = null, int? relatedInvoiceId = null, int? relatedProductId = null);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
    }
}
