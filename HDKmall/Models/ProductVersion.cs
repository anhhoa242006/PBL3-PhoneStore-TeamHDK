using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKmall.Models
{
    public class ProductVersion
    {
        [Key]
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        public string Name { get; set; } // e.g. "256GB", "512GB", "1TB"

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalPrice { get; set; }

        [NotMapped]
        public int DiscountPercent => OriginalPrice.HasValue && OriginalPrice > BasePrice 
            ? (int)Math.Round((double)(1 - (BasePrice / OriginalPrice.Value)) * 100) 
            : 0;

        public string? Description { get; set; } // "Đặc điểm nổi bật"

        public string? ImageUrl { get; set; } // Default image for this version

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();

        [NotMapped]
        public double AverageRating => Reviews != null && Reviews.Any(r => r.Status != "Hidden") 
            ? Reviews.Where(r => r.Status != "Hidden").Average(r => r.Rating) 
            : 0;
    }
}
