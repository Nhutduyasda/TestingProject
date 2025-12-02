using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MyProject.Areas.User.Models
{
    public class InvoiceDetail
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InvoiceDetailId { get; set; }
        
        [Required]
        public int InvoiceId { get; set; }
        
        /// <summary>
        /// Link to Variant if customer ordered individual product/variant
        /// NULL if customer ordered a Combo
        /// </summary>
        public int? VariantId { get; set; }
        
        /// <summary>
        /// Link to Combo if customer ordered a combo
        /// NULL if customer ordered individual product/variant
        /// Note: VariantId and ComboId are mutually exclusive (only one should have value)
        /// </summary>
        public int? ComboId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
        
        /// <summary>
        /// Unit price at time of purchase
        /// For variant: the variant price
        /// For combo: the combo price
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        
        /// <summary>
        /// Item type indicator: "Product" for variant, "Combo" for combo
        /// Used for efficient querying without joins
        /// </summary>
        [MaxLength(20)]
        public string ItemType { get; set; } = "Product"; // "Product" or "Combo"
        
        /// <summary>
        /// Additional notes for this line item (e.g., "extra sauce", "no onions")
        /// </summary>
        [MaxLength(200)]
        public string? Notes { get; set; }
        
        // Navigation properties
        [ValidateNever]
        public Invoice? Invoice { get; set; }
        
        [ValidateNever]
        public MyProject.Models.Shared.Variant? Variant { get; set; }
        
        [ValidateNever]
        public MyProject.Models.Shared.Combo? Combo { get; set; }
        
        /// <summary>
        /// Calculate total price for this line item
        /// </summary>
        public decimal GetLineTotal()
        {
            return UnitPrice * Quantity;
        }
        
        /// <summary>
        /// Validate that either VariantId or ComboId is set (but not both)
        /// </summary>
        public bool IsValid()
        {
            return (VariantId.HasValue && !ComboId.HasValue) || (!VariantId.HasValue && ComboId.HasValue);
        }
    }
}
