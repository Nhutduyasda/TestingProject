using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyProject.Areas.Admin.Models
{
    public enum OrderStatus
    {
        Pending = 0,        // User created, awaiting staff/admin confirmation
        Confirmed = 1,      // Confirmed by staff/admin
        Shipped = 2,        // Marked as shipped by staff/admin
        Completed = 3,      // User confirmed received
        CancelRequested = 4,// User requested cancel (before shipped)
        Cancelled = 5       
    }

    /// <summary>
    /// Audit log for tracking all order status changes
    /// Provides complete audit trail for compliance and debugging
    /// </summary>
    public class OrderAuditLog
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AuditId { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        public OrderStatus OldStatus { get; set; }

        [Required]
        public OrderStatus NewStatus { get; set; }

        [Required]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User ID who made the change (can be Customer, Staff, or Admin)
        /// </summary>
        public int? ChangedByUserId { get; set; }

        /// <summary>
        /// Role of the user who made the change
        /// </summary>
        [MaxLength(50)]
        public string? ChangedByRole { get; set; }

        /// <summary>
        /// IP Address of the user who made the change
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Reason for the status change
        /// </summary>
        [MaxLength(500)]
        public string? Reason { get; set; }

        /// <summary>
        /// Additional notes or comments
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Whether this was an automated change or manual
        /// </summary>
        public bool IsAutomated { get; set; }

        // Navigation properties
        [ForeignKey("InvoiceId")]
        public virtual MyProject.Areas.User.Models.Invoice? Invoice { get; set; }
    }
}
