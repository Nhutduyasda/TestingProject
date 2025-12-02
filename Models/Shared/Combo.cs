using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MyProject.Models.Shared
{
    /// <summary>
    /// Represents a Combo - a bundled set of products at a discounted price
    /// Used for promoting quick meal packages with predefined items
    /// </summary>
    public class Combo
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ComboId { get; set; }
        
        [Required, MaxLength(150)]
        public string ComboName { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        /// <summary>
        /// Base price of the combo (selling price)
        /// </summary>
        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        /// <summary>
        /// Original combined price of individual products in the combo
        /// Helps calculate discount percentage
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalPrice { get; set; }
        
        /// <summary>
        /// Discount percentage or amount (for display purposes)
        /// </summary>
        [NotMapped]
        public decimal DiscountAmount => (OriginalPrice ?? 0) - Price;
        
        [NotMapped]
        public decimal DiscountPercentage => OriginalPrice.HasValue && OriginalPrice > 0 
            ? ((OriginalPrice.Value - Price) / OriginalPrice.Value) * 100 
            : 0;
        
        /// <summary>
        /// Image URL for combo display
        /// </summary>
        [MaxLength(500)]
        public string? ImageUrl { get; set; }
        
        /// <summary>
        /// Category/Type of combo (e.g., "Breakfast Combo", "Lunch Special", etc.)
        /// </summary>
        [Required]
        public int CategoryId { get; set; }
        
        /// <summary>
        /// Is this combo currently available for ordering
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Track stock availability if combo has limited quantity
        /// If null, considered unlimited
        /// </summary>
        public int? AvailableQuantity { get; set; }
        
        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Last modification timestamp
        /// </summary>
        public DateTime? ModifiedDate { get; set; }
        
        /// <summary>
        /// Soft delete flag
        /// </summary>
        public bool IsDeleted { get; set; } = false;
        
        // Navigation properties
        [ValidateNever]
        public Categories? Category { get; set; }
        
        /// <summary>
        /// Collection of products included in this combo
        /// </summary>
        [ValidateNever]
        public ICollection<ComboProduct> ComboProducts { get; set; } = new List<ComboProduct>();
        
        /// <summary>
        /// Orders that include this combo (many-to-many through InvoiceDetail)
        /// </summary>
        [ValidateNever]
        public ICollection<MyProject.Areas.User.Models.InvoiceDetail> InvoiceDetails { get; set; } = new List<MyProject.Areas.User.Models.InvoiceDetail>();
        
        /// <summary>
        /// Calculate total price with optional quantity
        /// </summary>
        public decimal GetTotalPrice(int quantity = 1)
        {
            return Price * quantity;
        }
        
        /// <summary>
        /// Check if combo is available for purchase
        /// </summary>
        public bool IsAvailableForPurchase()
        {
            return IsActive && !IsDeleted && (!AvailableQuantity.HasValue || AvailableQuantity > 0);
        }
    }
}
