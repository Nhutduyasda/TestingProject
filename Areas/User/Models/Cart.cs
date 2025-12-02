using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyProject.Areas.User.Models
{
    public class Cart
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public MyProject.Areas.Admin.Models.User User { get; set; } = null!;
        public ICollection<CartDetail> CartDetails { get; set; } = new List<CartDetail>();
    }
}
