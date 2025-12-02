using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyProject.Models.Shared
{
    /// <summary>
    /// Hình ảnh của sản phẩm/variant
    /// </summary>
    public class ProductImage
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ImageId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        public int? VariantId { get; set; }
        
        [Required, StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? AltText { get; set; }
        
        public bool IsPrimary { get; set; } = false; 
        
        public int DisplayOrder { get; set; }
        
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public Product? Product { get; set; }
        public Variant? Variant { get; set; }
    }
}
