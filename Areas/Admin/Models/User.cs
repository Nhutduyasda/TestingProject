using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyProject.Areas.Admin.Models
{
    public enum UserRole
    {
        Customer = 0,
        Staff = 1,
        Admin = 2
    }

    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserID { get; set; }
        
        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [Required, StringLength(100)]
        public string FirstMidName { get; set; } = string.Empty;

        [NotMapped]
        public string FullName => $"{LastName} {FirstMidName}";
        
        [Required, StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required, StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Avatar { get; set; }
        
        [StringLength(200)]
        public string? StreetAddress { get; set; }
        
        [StringLength(100)]
        public string? Province { get; set; }
        
        [StringLength(100)]
        public string? District { get; set; }
        
        public UserRole Role { get; set; } = UserRole.Customer;
        
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        
        public DateTime? LastLoginDate { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public ICollection<MyProject.Areas.User.Models.Invoice> Invoices { get; set; } = new List<MyProject.Areas.User.Models.Invoice>();
        public ICollection<MyProject.Areas.User.Models.Cart> Carts { get; set; } = new List<MyProject.Areas.User.Models.Cart>();
        public ICollection<MyProject.Areas.User.Models.ProductReview> ProductReviews { get; set; } = new List<MyProject.Areas.User.Models.ProductReview>();
    }
}
