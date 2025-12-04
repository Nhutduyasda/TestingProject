using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyProject.Areas.User.Models
{
    public class CartDetail
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartDetailId { get; set; }
        
        [Required]
        public int CartId { get; set; }
        
        // ✅ ONLY link to Variant - NO ProductId
        // Product information is accessed via Variant.Product
        // ✅ Link to Variant (Nullable if Combo is selected)
        public int? VariantId { get; set; }

        // ✅ Link to Combo (Nullable if Variant is selected)
        public int? ComboId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
        
        // Navigation properties
        public Cart Cart { get; set; } = null!;
        public MyProject.Models.Shared.Variant? Variant { get; set; }
        public MyProject.Models.Shared.Combo? Combo { get; set; }
    }
}
