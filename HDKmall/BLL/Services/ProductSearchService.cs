using HDKmall.BLL.Interfaces;
using HDKmall.DAL.Interfaces;
using HDKmall.Models;
using HDKmall.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HDKmall.BLL.Services
{
    public class ProductSearchService : IProductSearchService
    {
        private readonly IProductRepository _productRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly ILogger<ProductSearchService> _logger;

        public ProductSearchService(IProductRepository productRepository, IReviewRepository reviewRepository, ILogger<ProductSearchService> logger)
        {
            _productRepository = productRepository;
            _reviewRepository = reviewRepository;
            _logger = logger;
        }

        public PaginationVM SearchProducts(ProductFilterVM filter)
        {
            var products = _productRepository.GetAll();
            if (products == null) return new PaginationVM();
            // Only show active products to customers
            var query = products.Where(p => p.IsActive).AsQueryable();

            // Apply filters
            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            if (filter.BrandId.HasValue)
            {
                query = query.Where(p => p.BrandId == filter.BrandId.Value);
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p =>
                    (p.Versions.Any() && p.Versions.Any(v => v.BasePrice >= filter.MinPrice.Value)) ||
                    (!p.Versions.Any() && p.Price >= filter.MinPrice.Value));
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p =>
                    (p.Versions.Any() && p.Versions.Any(v => v.BasePrice <= filter.MaxPrice.Value)) ||
                    (!p.Versions.Any() && p.Price <= filter.MaxPrice.Value));
            }

            if (!string.IsNullOrEmpty(filter.SearchQuery))
            {
                var searchTerm = filter.SearchQuery.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(searchTerm) || 
                                         (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                                         p.Versions.Any(v => v.Name.ToLower().Contains(searchTerm)));
            }

            if (filter.OnlyPromotions)
            {
                query = query.Where(p => p.Versions.Any(v => v.OriginalPrice != null && v.OriginalPrice > v.BasePrice));
            }

            // Evaluate the filtered products into memory first to avoid Expression Tree errors
            var allFilteredProducts = query.ToList();

            // Flatten products into versions in memory and map to VM
            var flattenedVMs = allFilteredProducts.SelectMany(p => 
            {
                var versions = p.Versions.AsEnumerable();
                if (filter.OnlyPromotions)
                {
                    versions = versions.Where(v => v.OriginalPrice != null && v.OriginalPrice > v.BasePrice);
                }

                if (versions.Any())
                {
                    return versions.Select(v => new { Product = p, Version = (ProductVersion?)v });
                }
                return new[] { new { Product = p, Version = (ProductVersion?)null } };
            }).Select(x => new ProductListVM
            {
                Id = x.Product.Id,
                VersionId = x.Version?.Id,
                Name = x.Product.Name,
                Slug = x.Product.Slug,
                Price = x.Version != null && x.Version.BasePrice > 0 ? x.Version.BasePrice : x.Product.Price,
                Description = x.Version?.Description ?? x.Product.Description ?? "",
                ImageUrl = x.Version?.ImageUrl ?? x.Product.ImageUrl ?? "/img/default.png",
                CategoryId = x.Product.CategoryId,
                CategoryName = x.Product.Category?.Name,
                BrandId = x.Product.BrandId,
                BrandName = x.Product.Brand?.Name,
                AverageRating = x.Product.Versions != null && x.Product.Versions.SelectMany(v => v.Reviews).Any(r => r.Status != "Hidden")
                    ? x.Product.Versions.SelectMany(v => v.Reviews).Where(r => r.Status != "Hidden").Average(r => r.Rating)
                    : 0,
                ReviewCount = x.Product.Versions != null
                    ? x.Product.Versions.SelectMany(v => v.Reviews).Count(r => r.Status != "Hidden")
                    : 0,
                VersionName = x.Version?.Name,
                OriginalPrice = x.Version?.OriginalPrice,
                DiscountPercent = x.Version?.DiscountPercent ?? 0
            }).ToList();

            // Apply sorting on the view models
            IEnumerable<ProductListVM> sortedVMs = flattenedVMs;
            switch (filter.SortBy)
            {
                case "price-low":
                    sortedVMs = flattenedVMs.OrderBy(x => x.Price);
                    break;
                case "price-high":
                    sortedVMs = flattenedVMs.OrderByDescending(x => x.Price);
                    break;
                case "rating":
                    sortedVMs = flattenedVMs.OrderByDescending(x => x.AverageRating);
                    break;
                default: // newest
                    sortedVMs = flattenedVMs.OrderByDescending(x => x.Id);
                    break;
            }

            var totalCount = sortedVMs.Count();
            var paged = sortedVMs
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return new PaginationVM
            {
                Products = paged,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public List<ProductListVM> GetProductsByCategory(int categoryId)
        {
            var products = _productRepository.GetAll()
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .ToList();

            return MapToProductListVM(products);
        }

        public List<ProductListVM> GetProductsByBrand(int brandId)
        {
            var products = _productRepository.GetAll()
                .Where(p => p.BrandId == brandId && p.IsActive)
                .ToList();

            return MapToProductListVM(products);
        }

        public List<ProductListVM> GetFeaturedProducts(int take = 10)
        {
            var products = _productRepository.GetAll()
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Versions.SelectMany(v => v.Reviews).Count())
                .ToList();

            var flat = MapToProductListVM(products);
            return flat.Take(take).ToList();
        }

        public List<ProductListVM> GetNewProducts(int take = 10)
        {
            var products = _productRepository.GetAll()
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Id)
                .ToList();

            var flat = MapToProductListVM(products);
            return flat.Take(take).ToList();
        }

        public ProductDetailVM GetProductDetail(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null || !product.IsActive) return null;
            return MapToProductDetailVM(product);
        }

        public ProductDetailVM GetProductDetailBySlug(string slug)
        {
            var product = _productRepository.GetBySlug(slug);
            if (product == null || !product.IsActive) return null;
            return MapToProductDetailVM(product);
        }

        private ProductDetailVM MapToProductDetailVM(Product product)
        {
            if (product == null) return null;

            var versions = product.Versions?.ToList() ?? new List<ProductVersion>();
            if (!versions.Any())
            {
                versions = new List<ProductVersion>
                {
                    new ProductVersion
                    {
                        Id = 0,
                        ProductId = product.Id,
                        Name = "Mặc định",
                        BasePrice = product.Price,
                        OriginalPrice = product.OriginalPrice,
                        Description = product.Description,
                        ImageUrl = product.ImageUrl,
                        Variants = new List<ProductVariant>(),
                        Specifications = new List<ProductSpecification>(),
                        Reviews = new List<Review>()
                    }
                };
            }

            var allReviews = versions.SelectMany(v => v.Reviews)
                .Where(r => r.Status != "Hidden")
                .ToList();

            return new ProductDetailVM
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description,
                ProductType = product.ProductType,
                BasePrice = product.Price,
                ImageUrl = product.ImageUrl,
                CategoryId = product.CategoryId,
                Category = product.Category,
                BrandId = product.BrandId,
                Brand = product.Brand,
                Versions = versions.Select(v => new ProductVersionVM
                {
                    Id = v.Id,
                    Name = v.Name,
                    BasePrice = v.BasePrice,
                    OriginalPrice = v.OriginalPrice,
                    DiscountPercent = v.DiscountPercent,
                    Description = v.Description,
                    ImageUrl = v.ImageUrl,
                    Variants = v.Variants.Select(vr => new ProductVariantVM
                    {
                        Id = vr.Id,
                        ProductVersionId = vr.ProductVersionId,
                        Color = vr.Color,
                        Price = vr.Price,
                        Stock = vr.Stock,
                        ImageUrl = vr.ImageUrl
                    }).ToList(),
                    Specifications = v.Specifications.OrderBy(s => s.DisplayOrder).Select(s => new ProductSpecVM
                    {
                        Id = s.Id,
                        SpecName = s.SpecName,
                        SpecValue = s.SpecValue,
                        DisplayOrder = s.DisplayOrder
                    }).ToList(),
                    AverageRating = v.Reviews != null && v.Reviews.Any(r => r.Status != "Hidden")
                        ? v.Reviews.Where(r => r.Status != "Hidden").Average(r => r.Rating)
                        : 0,
                    ReviewCount = v.Reviews != null ? v.Reviews.Count(r => r.Status != "Hidden") : 0
                }).ToList(),
                Reviews = allReviews.Select(r => new ReviewVM
                {
                    Id = r.Id,
                    ProductVersionId = r.ProductVersionId,
                    UserId = r.UserId,
                    UserName = r.User?.FullName ?? "Anonymous",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    Tags = r.Tags,
                    ImageUrl = r.ImageUrl,
                    IsEdited = r.IsEdited,
                    AdminReply = r.AdminReply,
                    AdminReplyAt = r.AdminReplyAt,
                    ProductVersionName = r.ProductVersion?.Name ?? versions.FirstOrDefault(ver => ver.Id == r.ProductVersionId)?.Name
                }).ToList(),
                AverageRating = allReviews.Any() ? allReviews.Average(r => r.Rating) : 0,
                TotalReviews = allReviews.Count,
                Images = (product.Images ?? new List<ProductImage>())
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ProductImageVM
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        IsMain = i.IsMain,
                        DisplayOrder = i.DisplayOrder
                    }).ToList()
            };
        }

        private List<ProductListVM> MapToProductListVM(List<Product> products)
        {
            var flattened = products.SelectMany(p => 
            {
                if (p.Versions != null && p.Versions.Any())
                {
                    return p.Versions.Select(v => new { Product = p, Version = (ProductVersion?)v });
                }
                return new[] { new { Product = p, Version = (ProductVersion?)null } };
            }).ToList();

            return flattened.Select(x => new ProductListVM
            {
                Id = x.Product.Id,
                VersionId = x.Version?.Id,
                Name = x.Product.Name,
                Slug = x.Product.Slug,
                Price = x.Version != null && x.Version.BasePrice > 0 ? x.Version.BasePrice : x.Product.Price,
                Description = x.Version?.Description ?? x.Product.Description ?? "",
                ImageUrl = x.Version?.ImageUrl ?? x.Product.ImageUrl ?? "/img/default.png",
                CategoryId = x.Product.CategoryId,
                CategoryName = x.Product.Category?.Name,
                BrandId = x.Product.BrandId,
                BrandName = x.Product.Brand?.Name,
                AverageRating = x.Product.Versions != null && x.Product.Versions.SelectMany(v => v.Reviews).Any(r => r.Status != "Hidden")
                    ? x.Product.Versions.SelectMany(v => v.Reviews).Where(r => r.Status != "Hidden").Average(r => r.Rating)
                    : 0,
                ReviewCount = x.Product.Versions != null
                    ? x.Product.Versions.SelectMany(v => v.Reviews).Count(r => r.Status != "Hidden")
                    : 0,
                VersionName = x.Version?.Name,
                OriginalPrice = x.Version?.OriginalPrice ?? x.Product.OriginalPrice,
                DiscountPercent = x.Version != null && x.Version.OriginalPrice > 0 && x.Version.BasePrice > 0
                    ? x.Version.DiscountPercent
                    : (x.Product.OriginalPrice.HasValue && x.Product.OriginalPrice > x.Product.Price && x.Product.Price > 0
                        ? (int)Math.Round((double)(1 - (double)(x.Product.Price / x.Product.OriginalPrice.Value)) * 100)
                        : 0)
            }).ToList();
        }
    }
}
