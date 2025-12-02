using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyProject.Models.Shared
{
    /// <summary>
    /// Định nghĩa các loại thuộc tính có thể có cho variant
    /// VD: Size, Color, Weight, Age, Gender
    /// </summary>
    public class AttributeType
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AttributeTypeId { get; set; }
        
        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty; // Size, Color, Weight, Age...
        
        [StringLength(200)]
        public string? Description { get; set; }
        
        // Display order for UI
        public int DisplayOrder { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation property
        public ICollection<VariantAttribute> VariantAttributes { get; set; } = new List<VariantAttribute>();
    }
}
