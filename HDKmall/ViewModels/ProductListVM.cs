using HDKmall.Models;

namespace HDKmall.ViewModels
{
    public class ProductListVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Slug { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int? BrandId { get; set; }
        public string BrandName { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        
        public int? VersionId { get; set; }
        public string? VersionName { get; set; }
        public decimal? OriginalPrice { get; set; }
        public int DiscountPercent { get; set; }
    }
}
