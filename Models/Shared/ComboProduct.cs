    using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MyProject.Models.Shared
{
    /// <summary>
    /// Junction table for Combo and Product relationship
    /// Represents which products are included in a combo
    /// </summary>
    public class ComboProduct
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ComboProductId { get; set; }
        
        [Required]
        public int ComboId { get; set; }
        
        [Required]
        public int ProductId { get; set; }

        public int? VariantId { get; set; }
        
        /// <summary>
        /// Quantity of this product included in the combo
        /// </summary>
        [Required]
        public int Quantity { get; set; } = 1;
        
        /// <summary>
        /// Individual price of product in this combo (for reference/audit)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        
        /// <summary>
        /// Notes or specifications for this product in the combo (e.g., "Extra sauce")
        /// </summary>
        [MaxLength(200)]
        public string? Notes { get; set; }
        
        /// <summary>
        /// Order of display in the combo
        /// </summary>
        public int DisplayOrder { get; set; } = 0;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [ValidateNever]
        public Combo? Combo { get; set; }
        
        [ValidateNever]
        public Product? Product { get; set; }

        [ValidateNever]
        public Variant? Variant { get; set; }
    }
}
