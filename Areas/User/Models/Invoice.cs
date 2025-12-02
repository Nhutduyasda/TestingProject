using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MyProject.Areas.User.Models
{
    public enum PayMethod
    {
        Cash,
        CreditCard,
        DebitCard,
        MobilePayment
    }

    public class Invoice
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InvoiceId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        public PayMethod? PayMethod { get; set; }
        
        [Required]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Delivery information
        [Required]
        [StringLength(100)]
        public string RecipientName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string DeliveryAddress { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Note { get; set; }
        
        // Order workflow & status tracking
        public MyProject.Areas.Admin.Models.OrderStatus Status { get; set; } = MyProject.Areas.Admin.Models.OrderStatus.Pending;
        
        public bool IsDeleted { get; set; } = false;
        
        [StringLength(500)]
        public string? CancelReason { get; set; }
        
        // Timestamp tracking for each status
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        
        // Navigation properties
        [ValidateNever]
        public MyProject.Areas.Admin.Models.User User { get; set; } = null!;
        
        [ValidateNever]
        public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    }
}
