using HDKmall.BLL.Interfaces;
using HDKmall.Models;
using HDKmall.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HDKmall.BLL.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;

        public WishlistService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ToggleWishlistAsync(int userId, int productId, int? versionId)
        {
            var existing = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId && w.VersionId == versionId);

            if (existing != null)
            {
                _context.Wishlists.Remove(existing);
                await _context.SaveChangesAsync();
                return false; // Removed
            }
            else
            {
                var wishlist = new Wishlist
                {
                    UserId = userId,
                    ProductId = productId,
                    VersionId = versionId,
                    AddedDate = HDKmall.Helpers.TimeHelper.GetVietnamTime()
                };
                _context.Wishlists.Add(wishlist);
                await _context.SaveChangesAsync();
                return true; // Added
            }
        }

        public async Task<List<ProductListVM>> GetUserWishlistAsync(int userId)
        {
            var wishlistItems = await _context.Wishlists
                .Include(w => w.Product)
                    .ThenInclude(p => p.Brand)
                .Include(w => w.Product)
                    .ThenInclude(p => p.Images)
                .Include(w => w.Product)
                    .ThenInclude(p => p.Versions)
                        .ThenInclude(v => v.Reviews)
                .Include(w => w.ProductVersion)
                    .ThenInclude(v => v.Reviews)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedDate)
                .ToListAsync();

            return wishlistItems.Select(w => new ProductListVM
            {
                Id = w.ProductId,
                Name = w.Product.Name,
                Slug = w.Product.Slug,
                Price = w.ProductVersion?.BasePrice ?? w.Product.Price,
                OriginalPrice = w.ProductVersion?.OriginalPrice,
                DiscountPercent = w.ProductVersion != null && w.ProductVersion.OriginalPrice > 0 
                    ? (int)Math.Round((double)(1 - (w.ProductVersion.BasePrice / w.ProductVersion.OriginalPrice)) * 100) 
                    : 0,
                ImageUrl = w.ProductVersion?.ImageUrl ?? w.Product.Images.FirstOrDefault()?.ImageUrl ?? w.Product.ImageUrl ?? "/img/default.png",
                BrandName = w.Product.Brand?.Name ?? "",
                VersionId = w.VersionId,
                VersionName = w.ProductVersion?.Name ?? "",
                AverageRating = w.ProductVersion != null ? w.ProductVersion.AverageRating : (w.Product.Versions.Any() ? w.Product.Versions.Average(v => v.AverageRating) : 0),
                ReviewCount = w.ProductVersion != null ? w.ProductVersion.Reviews.Count : w.Product.Versions.Sum(v => v.Reviews.Count)
            }).ToList();
        }

        public async Task<bool> IsInWishlistAsync(int userId, int productId, int? versionId)
        {
            return await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId && w.VersionId == versionId);
        }

        public async Task<int> GetWishlistCountAsync(int userId)
        {
            return await _context.Wishlists.CountAsync(w => w.UserId == userId);
        }
    }
}
