using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MyProject.Areas.User.Models
{
    public class ProductReview
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReviewId { get; set; }
        
        [Required]
        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }
        
        [Required]
        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "Rating is required.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }
        
        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters.")]
        public string? Comment { get; set; }

        [StringLength(1000, ErrorMessage = "Review text cannot exceed 1000 characters.")]
        public string? ReviewText { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsApproved { get; set; } = false;
        
        // Navigation properties
        [ValidateNever]
        public MyProject.Models.Shared.Product? Product { get; set; }
        
        [ValidateNever]
        public MyProject.Areas.Admin.Models.User? User { get; set; }
    }
}
