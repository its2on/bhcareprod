using Microsoft.AspNetCore.Mvc;
using Barangay.Services;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Barangay.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationBellViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return View("Default", new { Count = 0, Notifications = new List<object>() });
            }

            var unreadNotifications = await _notificationService.GetUnreadNotificationsAsync(userId);
            var unreadCount = unreadNotifications.Count;

            return View("Default", new { Count = unreadCount, Notifications = unreadNotifications });
        }
    }
}
