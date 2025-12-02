using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyProject.Models.Shared
{
    /// <summary>
    /// Thuộc tính cụ thể của một variant
    /// VD: Size = "Large", Color = "Red", Weight = "5kg"
    /// </summary>
    public class VariantAttribute
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VariantAttributeId { get; set; }
        
        [Required]
        public int VariantId { get; set; }
        
        [Required]
        public int AttributeTypeId { get; set; }
        
        [Required, StringLength(100)]
        public string Value { get; set; } = string.Empty; // "Large", "Red", "5kg", "3 months"...
        
        // Navigation properties
        public Variant? Variant { get; set; }
        public AttributeType? AttributeType { get; set; }
    }
}
