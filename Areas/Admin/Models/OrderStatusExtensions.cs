namespace MyProject.Areas.Admin.Models
{
    /// <summary>
    /// Extension methods for OrderStatus enum to provide display text and validation logic
    /// </summary>
    public static class OrderStatusExtensions
    {
        /// <summary>
        /// Get Vietnamese display text for order status
        /// </summary>
        public static string ToDisplayText(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Chờ xử lý",
                OrderStatus.Confirmed => "Đã xác nhận",
                OrderStatus.Shipped => "Đang giao hàng",
                OrderStatus.Completed => "Hoàn thành",
                OrderStatus.CancelRequested => "Yêu cầu hủy",
                OrderStatus.Cancelled => "Đã hủy",
                _ => "Không xác định"
            };
        }

        public static string GetDisplayName(this OrderStatus status)
        {
            return ToDisplayText(status);
        }

        /// <summary>
        /// Get Bootstrap color class for status badge
        /// </summary>
        public static string ToColorClass(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "warning",
                OrderStatus.Confirmed => "info",
                OrderStatus.Shipped => "primary",
                OrderStatus.Completed => "success",
                OrderStatus.CancelRequested => "warning",
                OrderStatus.Cancelled => "danger",
                _ => "secondary"
            };
        }

        public static string GetCssClass(this OrderStatus status)
        {
            return $"badge bg-{ToColorClass(status)}";
        }

        /// <summary>
        /// Get FontAwesome icon class for status
        /// </summary>
        public static string ToIconClass(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "fas fa-clock",
                OrderStatus.Confirmed => "fas fa-check-circle",
                OrderStatus.Shipped => "fas fa-truck",
                OrderStatus.Completed => "fas fa-flag-checkered",
                OrderStatus.CancelRequested => "fas fa-exclamation-triangle",
                OrderStatus.Cancelled => "fas fa-times-circle",
                _ => "fas fa-question-circle"
            };
        }

        /// <summary>
        /// Check if order can transition to target status - State machine validation
        /// </summary>
        public static bool CanTransitionTo(this OrderStatus current, OrderStatus target)
        {
            return (current, target) switch
            {
                // From Pending
                (OrderStatus.Pending, OrderStatus.Confirmed) => true,
                (OrderStatus.Pending, OrderStatus.Cancelled) => true,
                (OrderStatus.Pending, OrderStatus.CancelRequested) => true,
                
                // From Confirmed
                (OrderStatus.Confirmed, OrderStatus.Shipped) => true,
                (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
                (OrderStatus.Confirmed, OrderStatus.CancelRequested) => true,
                
                // From Shipped
                (OrderStatus.Shipped, OrderStatus.Completed) => true,
                
                // From CancelRequested
                (OrderStatus.CancelRequested, OrderStatus.Cancelled) => true,
                (OrderStatus.CancelRequested, OrderStatus.Pending) => true,
                (OrderStatus.CancelRequested, OrderStatus.Confirmed) => true,
                
                // Default: no transition allowed
                _ => false
            };
        }

        /// <summary>
        /// Check if customer can request cancel for this status
        /// </summary>
        public static bool CanCustomerCancel(this OrderStatus status)
        {
            return status == OrderStatus.Pending || status == OrderStatus.Confirmed;
        }

        /// <summary>
        /// Check if status is a final state (no more changes allowed)
        /// </summary>
        public static bool IsFinalStatus(this OrderStatus status)
        {
            return status == OrderStatus.Completed || status == OrderStatus.Cancelled;
        }

        /// <summary>
        /// Check if admin can manually update this status
        /// </summary>
        public static bool IsAdminEditable(this OrderStatus status)
        {
            return status != OrderStatus.Completed && status != OrderStatus.Cancelled;
        }

        /// <summary>
        /// Get next possible statuses for admin workflow
        /// </summary>
        public static OrderStatus[] GetNextStatuses(this OrderStatus current)
        {
            return current switch
            {
                OrderStatus.Pending => new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
                OrderStatus.Confirmed => new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
                OrderStatus.Shipped => new[] { OrderStatus.Completed },
                OrderStatus.CancelRequested => new[] { OrderStatus.Cancelled, OrderStatus.Confirmed },
                _ => Array.Empty<OrderStatus>()
            };
        }

        /// <summary>
        /// Calculate progress percentage for timeline (0-100)
        /// </summary>
        public static int GetProgressPercentage(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => 25,
                OrderStatus.Confirmed => 50,
                OrderStatus.Shipped => 75,
                OrderStatus.Completed => 100,
                OrderStatus.Cancelled => 0,
                OrderStatus.CancelRequested => 0,
                _ => 0
            };
        }
    }
}
