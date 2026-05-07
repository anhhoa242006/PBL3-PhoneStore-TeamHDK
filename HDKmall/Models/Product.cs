using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKmall.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Slug { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalPrice { get; set; }

        public int ProductType { get; set; } = 1; // 1: HasVersions, 2: ColorsOnly
        public bool IsActive { get; set; } = true;

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }
        
        public string? PublicId { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public int? BrandId { get; set; }
        public Brand? Brand { get; set; }

        public ICollection<ProductVersion> Versions { get; set; } = new List<ProductVersion>();
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    }
}