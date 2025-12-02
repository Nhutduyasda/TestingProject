using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MyProject.Areas.User.Models
{
    public class Wishlist
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int WishlistId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        public DateTime AddedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        [ValidateNever]
        public MyProject.Areas.Admin.Models.User? User { get; set; }
        
        [ValidateNever]
        public MyProject.Models.Shared.Product? Product { get; set; }
    }
}
