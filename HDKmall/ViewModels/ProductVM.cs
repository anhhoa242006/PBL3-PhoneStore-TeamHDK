using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace HDKmall.ViewModels
{
    /// <summary>
    /// Loại cấu trúc sản phẩm
    /// </summary>
    public enum ProductType
    {
        HasVersions = 1,   // Điện thoại: có phiên bản dung lượng (256GB, 512GB...)
        ColorsOnly  = 2    // Loa, tai nghe: chỉ màu sắc, giá lưu ở Product.Price hoặc Variant.Price
    }

    public class ProductVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        public string Name { get; set; }

        /// <summary>Loại sản phẩm — quyết định cấu trúc giá và phiên bản</summary>
        public ProductType ProductType { get; set; } = ProductType.HasVersions;

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Giá trực tiếp – dùng cho ColorsOnly và Simple.
        /// Với HasVersions, giá lưu trong Version.BasePrice.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>Giá chưa giảm (để hiển thị giá gạch đi)</summary>
        public decimal? OriginalPrice { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }

        public int? BrandId { get; set; }

        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }

        public List<IFormFile>? GalleryFiles { get; set; } = new List<IFormFile>();
        public List<ProductImageVM>? ExistingImages { get; set; } = new List<ProductImageVM>();

        // Chỉ dùng khi ProductType = HasVersions
        public List<ProductVersionVM> Versions { get; set; } = new List<ProductVersionVM>();
    }
}
