using HDKmall.BLL.Interfaces;
using HDKmall.DAL.Interfaces;
using HDKmall.Models;
using HDKmall.ViewModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HDKmall.BLL.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly IProductRepository _productRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private const string RECENTLY_VIEWED_COOKIE = "RecentlyViewedProducts";
        private const string RECENT_SEARCHES_COOKIE = "RecentSearches";

        public RecommendationService(IProductRepository productRepository, IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public void AddToRecentlyViewed(int productId)
        {
            var cookie = _httpContextAccessor.HttpContext?.Request.Cookies[RECENTLY_VIEWED_COOKIE];
            List<int> productIds = new List<int>();

            if (!string.IsNullOrEmpty(cookie))
            {
                productIds = cookie.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(int.Parse)
                                  .ToList();
            }

            // Remove if already exists to move it to the front
            productIds.Remove(productId);
            productIds.Insert(0, productId);

            // Keep only top 15
            productIds = productIds.Take(15).ToList();

            var options = new CookieOptions { Expires = DateTime.Now.AddDays(30), Path = "/" };
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(RECENTLY_VIEWED_COOKIE, string.Join(",", productIds), options);
        }

        public List<ProductListVM> GetRecentlyViewedProducts(int take = 10)
        {
            var cookie = _httpContextAccessor.HttpContext?.Request.Cookies[RECENTLY_VIEWED_COOKIE];
            if (string.IsNullOrEmpty(cookie)) return new List<ProductListVM>();

            var productIds = cookie.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(int.Parse)
                                  .Take(take)
                                  .ToList();

            var products = _productRepository.GetAll()
                                           .Where(p => productIds.Contains(p.Id))
                                           .ToList();

            // Sort according to the order in the cookie
            var sortedProducts = productIds.Select(id => products.FirstOrDefault(p => p.Id == id))
                                         .Where(p => p != null)
                                         .ToList();

            return MapToProductListVM(sortedProducts);
        }

        public void AddToRecentSearches(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;
            query = query.Trim();

            var cookie = _httpContextAccessor.HttpContext?.Request.Cookies[RECENT_SEARCHES_COOKIE];
            List<string> searches = new List<string>();

            if (!string.IsNullOrEmpty(cookie))
            {
                searches = cookie.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            searches.Remove(query);
            searches.Insert(0, query);
            searches = searches.Take(10).ToList();

            var options = new CookieOptions { Expires = DateTime.Now.AddDays(7), Path = "/" };
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(RECENT_SEARCHES_COOKIE, string.Join("|", searches), options);
        }

        public List<string> GetRecentSearches(int take = 5)
        {
            var cookie = _httpContextAccessor.HttpContext?.Request.Cookies[RECENT_SEARCHES_COOKIE];
            if (string.IsNullOrEmpty(cookie)) return new List<string>();

            return cookie.Split('|', StringSplitOptions.RemoveEmptyEntries).Take(take).ToList();
        }

        public List<ProductListVM> GetRelatedProducts(int productId, int take = 8)
        {
            var product = _productRepository.GetById(productId);
            if (product == null) return new List<ProductListVM>();

            var related = _productRepository.GetAll()
                                          .Where(p => p.Id != productId && 
                                                     (p.CategoryId == product.CategoryId || p.BrandId == product.BrandId))
                                          .OrderByDescending(p => p.CategoryId == product.CategoryId) // Prioritize same category
                                          .ThenByDescending(p => p.Id)
                                          .Take(take)
                                          .ToList();

            return MapToProductListVM(related);
        }

        public List<ProductListVM> GetRecommendationsForCart(List<int> productIdsInCart, int take = 10)
        {
            if (productIdsInCart == null || !productIdsInCart.Any())
            {
                return GetPersonalizedRecommendations(take);
            }

            // Get categories and brands from cart
            var productsInCart = _productRepository.GetAll()
                                                 .Where(p => productIdsInCart.Contains(p.Id))
                                                 .ToList();

            var categoryIds = productsInCart.Select(p => p.CategoryId).Distinct().ToList();
            var brandIds = productsInCart.Select(p => p.BrandId).Distinct().ToList();

            var recommendations = _productRepository.GetAll()
                                                  .Where(p => !productIdsInCart.Contains(p.Id) && 
                                                             (categoryIds.Contains(p.CategoryId) || brandIds.Contains(p.BrandId ?? 0)))
                                                  .OrderByDescending(p => categoryIds.Contains(p.CategoryId))
                                                  .ThenByDescending(p => p.Id)
                                                  .Take(take)
                                                  .ToList();

            return MapToProductListVM(recommendations);
        }

        public List<ProductListVM> GetPersonalizedRecommendations(int take = 10)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            List<int> wishlistedProductIds = new List<int>();

            if (user?.Identity?.IsAuthenticated == true)
            {
                var userIdStr = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    wishlistedProductIds = _context.Wishlists
                        .Where(w => w.UserId == userId)
                        .Select(w => w.ProductId)
                        .Distinct()
                        .ToList();
                }
            }

            // First priority: Wishlisted products (not yet in cart/viewed?)
            var wishlistProducts = _productRepository.GetAll()
                                                   .Where(p => wishlistedProductIds.Contains(p.Id))
                                                   .ToList();

            // Second priority: Recently viewed
            var recentlyViewed = GetRecentlyViewedProducts(5);
            
            // Third priority: High rated/Trending products
            var trending = _productRepository.GetAll()
                                           .OrderByDescending(p => p.Versions.SelectMany(v => v.Reviews).Count())
                                           .Take(take + 5) // Take more to filter
                                           .ToList();

            var result = MapToProductListVM(trending);
            
            // Mix in Wishlist
            if (wishlistProducts.Any())
            {
                var wpMapped = MapToProductListVM(wishlistProducts);
                var wpIds = wpMapped.Select(w => w.Id).ToList();
                result = result.Where(r => !wpIds.Contains(r.Id)).ToList();
                result.InsertRange(0, wpMapped);
            }

            // Mix in some recently viewed if available
            if (recentlyViewed.Any())
            {
                var rvIds = recentlyViewed.Select(r => r.Id).ToList();
                result = result.Where(r => !rvIds.Contains(r.Id)).ToList();
                result.InsertRange(0, recentlyViewed);
            }

            return result.DistinctBy(p => p.Id).Take(take).ToList();
        }

        private List<ProductListVM> MapToProductListVM(List<Product> products)
        {
            return products.Select(p => {
                // Pick the "best" version to show (cheapest/first)
                var bestVersion = p.Versions?.OrderBy(v => v.BasePrice).FirstOrDefault();
                
                return new ProductListVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    Price = (bestVersion != null && bestVersion.BasePrice > 0) ? bestVersion.BasePrice : p.Price,
                    ImageUrl = bestVersion?.ImageUrl ?? p.ImageUrl,
                    CategoryName = p.Category?.Name,
                    BrandName = p.Brand?.Name,
                    AverageRating = p.Versions != null && p.Versions.SelectMany(v => v.Reviews).Any() 
                        ? p.Versions.SelectMany(v => v.Reviews).Average(r => r.Rating) 
                        : 0,
                    ReviewCount = p.Versions != null ? p.Versions.SelectMany(v => v.Reviews).Count() : 0,
                    VersionId = bestVersion?.Id,
                    VersionName = bestVersion?.Name,
                    OriginalPrice = bestVersion?.OriginalPrice ?? p.OriginalPrice,
                    DiscountPercent = (bestVersion != null && bestVersion.OriginalPrice > 0 && bestVersion.BasePrice > 0)
                        ? bestVersion.DiscountPercent
                        : (p.OriginalPrice.HasValue && p.OriginalPrice > p.Price && p.Price > 0)
                            ? (int)Math.Round((double)(1 - (double)(p.Price / p.OriginalPrice.Value)) * 100)
                            : 0
                };
            }).ToList();
        }
    }
}
