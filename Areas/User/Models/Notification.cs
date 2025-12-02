using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MyProject.Areas.User.Models
{
    public class Notification
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NotificationId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required, StringLength(255)]
        public string Title { get; set; } = string.Empty;
        
        [Required, StringLength(500)]
        public string Message { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? Type { get; set; } // Order, Review, Promotion, etc.
        
        public int? RelatedInvoiceId { get; set; }
        
        public int? RelatedProductId { get; set; }
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        [ValidateNever]
        public MyProject.Areas.Admin.Models.User? User { get; set; }
        
        [ValidateNever]
        public Invoice? RelatedInvoice { get; set; }
        
        [ValidateNever]
        public MyProject.Models.Shared.Product? RelatedProduct { get; set; }
    }
}
