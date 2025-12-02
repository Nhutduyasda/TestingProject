using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MyProject.Models.Shared
{
    public class Product
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductId { get; set; }
        
        [Required]
        public string ProductName { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        public int SupplierId { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        
        public int Quanlity { get; set; }
        
        // Calculated property - total quantity from all variants
        [NotMapped]
        public int TotalQuantityFromVariants => Variants?.Sum(v => v.Quanlity) ?? 0;
        
        // Review statistics
        [NotMapped]
        public double AverageRating { get; set; }
        
        [NotMapped]
        public int ReviewCount { get; set; }
        
        // Navigation properties
        public ICollection<Variant> Variants { get; set; } = new List<Variant>();
        
        [ValidateNever]
        public Supplier? Supplier { get; set; }
        
        [ValidateNever]
        public Categories? Category { get; set; }
        
        // Product images and reviews
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<MyProject.Areas.User.Models.ProductReview> Reviews { get; set; } = new List<MyProject.Areas.User.Models.ProductReview>();
        
        // Method to sync quantity from variants
        public void UpdateQuantityFromVariants()
        {
            Quanlity = TotalQuantityFromVariants;
        }
    }
}
