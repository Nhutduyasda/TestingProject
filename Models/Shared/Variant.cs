using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MyProject.Models.Shared
{
    public class Variant
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VariantId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required(ErrorMessage = "Variant name is required.")]
        public string VariantName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }
        
        [Required(ErrorMessage = "Quantity is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
        public int Quanlity { get; set; }
        
        [Required]
        public bool IsActive { get; set; } = true;

        [Timestamp]
        public byte[] RowVersion { get; set; }

        // Navigation properties
        public virtual Product? Product { get; set; }
        
        public ICollection<MyProject.Areas.User.Models.CartDetail> CartDetails { get; set; } = new List<MyProject.Areas.User.Models.CartDetail>();
        public ICollection<MyProject.Areas.User.Models.InvoiceDetail> InvoiceDetails { get; set; } = new List<MyProject.Areas.User.Models.InvoiceDetail>();
        
        // Enhanced variant management
        public ICollection<VariantAttribute> VariantAttributes { get; set; } = new List<VariantAttribute>();
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<MyProject.Areas.Admin.Models.InventoryLog> InventoryLogs { get; set; } = new List<MyProject.Areas.Admin.Models.InventoryLog>();
    }
}
