using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyProject.Areas.Admin.Models
{
    public enum InventoryAction
    {
        Import,      
        Export,      
        Adjust,      
        Return,      
        Damaged     
    }

    /// <summary>
    /// Lịch sử nhập/xuất kho cho từng variant
    /// </summary>
    public class InventoryLog
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LogId { get; set; }
        
        [Required]
        public int VariantId { get; set; }
        
        [Required]
        public InventoryAction Action { get; set; }
        
        [Required]
        public int QuantityChange { get; set; } // Số lượng thay đổi (+ hoặc -)
        
        public int QuantityBefore { get; set; }
        
        public int QuantityAfter { get; set; }
        
        [StringLength(500)]
        public string? Reason { get; set; }
        
        public int? InvoiceId { get; set; } // Nếu là xuất hàng do bán
        
        public int? UserId { get; set; } // Người thực hiện
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public MyProject.Models.Shared.Variant? Variant { get; set; }
        public MyProject.Areas.User.Models.Invoice? Invoice { get; set; }
        public User? User { get; set; }
    }
}
