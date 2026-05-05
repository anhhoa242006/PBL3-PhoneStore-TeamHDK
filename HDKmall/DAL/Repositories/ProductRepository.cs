using HDKmall.DAL.Interfaces;
using HDKmall.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

namespace HDKmall.DAL.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Product> GetAll()
        {
            return _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Reviews)
                .ToList();
        }

        public Product GetById(int id)
        {
            return _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Variants)
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Specifications)
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Reviews)
                        .ThenInclude(r => r.User)
                .Include(p => p.Images)
                .FirstOrDefault(p => p.Id == id);
        }

        public Product GetBySlug(string slug)
        {
            return _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Variants)
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Specifications)
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Reviews)
                        .ThenInclude(r => r.User)
                .Include(p => p.Images)
                .FirstOrDefault(p => p.Slug == slug);
        }

        public void Add(Product product)
        {
            if (string.IsNullOrEmpty(product.Slug))
            {
                product.Slug = HDKmall.Helpers.SlugHelper.GenerateSlug(product.Name);
            }
            _context.Products.Add(product);
            _context.SaveChanges();
        }

        public void Update(Product product)
        {
            if (string.IsNullOrEmpty(product.Slug))
            {
                product.Slug = HDKmall.Helpers.SlugHelper.GenerateSlug(product.Name);
            }
            _context.Products.Update(product);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var product = _context.Products
                .Include(p => p.Images)
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Variants)
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Specifications)
                .Include(p => p.Versions)
                    .ThenInclude(v => v.Reviews)
                .FirstOrDefault(p => p.Id == id);

            if (product != null)
            {
                var versionIds = product.Versions.Select(v => v.Id).ToList();
                var variantIds = product.Versions.SelectMany(v => v.Variants).Select(vr => vr.Id).ToList();

                // Remove dependent records explicitly so deletes do not fail on FK restrictions.
                var cartItems = _context.CartItems
                    .Where(ci => ci.ProductId == id || (ci.VariantId.HasValue && variantIds.Contains(ci.VariantId.Value)))
                    .ToList();
                if (cartItems.Any()) _context.CartItems.RemoveRange(cartItems);

                var orderDetails = _context.OrderDetails
                    .Where(od => od.ProductId == id || (od.ProductVariantId.HasValue && variantIds.Contains(od.ProductVariantId.Value)))
                    .ToList();
                if (orderDetails.Any()) _context.OrderDetails.RemoveRange(orderDetails);

                var wishlists = _context.Wishlists
                    .Where(w => w.ProductId == id || (w.VersionId.HasValue && versionIds.Contains(w.VersionId.Value)))
                    .ToList();
                if (wishlists.Any()) _context.Wishlists.RemoveRange(wishlists);

                var reviews = _context.Reviews
                    .Where(r => versionIds.Contains(r.ProductVersionId))
                    .ToList();
                if (reviews.Any()) _context.Reviews.RemoveRange(reviews);

                var variants = _context.ProductVariants
                    .Where(v => versionIds.Contains(v.ProductVersionId))
                    .ToList();
                if (variants.Any()) _context.ProductVariants.RemoveRange(variants);

                var specs = _context.ProductSpecifications
                    .Where(s => versionIds.Contains(s.ProductVersionId))
                    .ToList();
                if (specs.Any()) _context.ProductSpecifications.RemoveRange(specs);

                var images = _context.ProductImages.Where(i => i.ProductId == id).ToList();
                if (images.Any()) _context.ProductImages.RemoveRange(images);

                var versions = _context.ProductVersions.Where(v => v.ProductId == id).ToList();
                if (versions.Any()) _context.ProductVersions.RemoveRange(versions);

                _context.Products.Remove(product);
                _context.SaveChanges();
            }
        }

        // Version methods
        public void AddVersion(ProductVersion version)
        {
            _context.ProductVersions.Add(version);
            _context.SaveChanges();
        }

        public void UpdateVersion(ProductVersion version)
        {
            _context.ProductVersions.Update(version);
            _context.SaveChanges();
        }

        public void DeleteVersionById(int versionId)
        {
            var version = _context.ProductVersions.Find(versionId);
            if (version != null)
            {
                _context.ProductVersions.Remove(version);
                _context.SaveChanges();
            }
        }

        public void DeleteVersions(int productId)
        {
            // Load versions with variants to ensure we have IDs for cleanup
            var versions = _context.ProductVersions
                .Include(v => v.Variants)
                .Where(v => v.ProductId == productId)
                .ToList();

            if (versions.Any())
            {
                var variantIds = versions.SelectMany(v => v.Variants).Select(vr => (int?)vr.Id).ToList();
                if (variantIds.Any())
                {
                    var cartItems = _context.CartItems.Where(ci => ci.VariantId.HasValue && variantIds.Contains(ci.VariantId)).ToList();
                    if (cartItems.Any()) _context.CartItems.RemoveRange(cartItems);
                }

                _context.ProductVersions.RemoveRange(versions);
                _context.SaveChanges();
            }
        }

        // Image methods
        public void AddImage(ProductImage image)
        {
            _context.ProductImages.Add(image);
            _context.SaveChanges();
        }

        public void DeleteImage(int imageId)
        {
            var image = _context.ProductImages.Find(imageId);
            if (image != null)
            {
                _context.ProductImages.Remove(image);
                _context.SaveChanges();
            }
        }

        // Variant methods
        public void AddVariant(ProductVariant variant)
        {
            _context.ProductVariants.Add(variant);
            _context.SaveChanges();
        }

        public void UpdateVariant(ProductVariant variant)
        {
            _context.ProductVariants.Update(variant);
            _context.SaveChanges();
        }

        public void DeleteVariantById(int variantId)
        {
            var variant = _context.ProductVariants.Find(variantId);
            if (variant != null)
            {
                _context.ProductVariants.Remove(variant);
                _context.SaveChanges();
            }
        }

        public void DeleteVariantsByVersionId(int versionId)
        {
            var variants = _context.ProductVariants.Where(v => v.ProductVersionId == versionId).ToList();
            if (variants.Any())
            {
                _context.ProductVariants.RemoveRange(variants);
                _context.SaveChanges();
            }
        }

        public void DeleteVariants(int productId)
        {
            // This is a bit tricky now since variants are linked to versions
            var variants = _context.ProductVariants.Where(v => v.ProductVersion.ProductId == productId).ToList();
            _context.ProductVariants.RemoveRange(variants);
            _context.SaveChanges();
        }

        // Specification methods
        public void AddSpecification(ProductSpecification spec)
        {
            _context.ProductSpecifications.Add(spec);
            _context.SaveChanges();
        }

        public void UpdateSpecification(ProductSpecification spec)
        {
            _context.ProductSpecifications.Update(spec);
            _context.SaveChanges();
        }

        public void DeleteSpecById(int specId)
        {
            var spec = _context.ProductSpecifications.Find(specId);
            if (spec != null)
            {
                _context.ProductSpecifications.Remove(spec);
                _context.SaveChanges();
            }
        }

        public void DeleteSpecificationsByVersionId(int versionId)
        {
            var specs = _context.ProductSpecifications.Where(s => s.ProductVersionId == versionId).ToList();
            if (specs.Any())
            {
                _context.ProductSpecifications.RemoveRange(specs);
                _context.SaveChanges();
            }
        }

        public void DeleteSpecifications(int productId)
        {
            var specs = _context.ProductSpecifications.Where(s => s.ProductVersion.ProductId == productId).ToList();
            _context.ProductSpecifications.RemoveRange(specs);
            _context.SaveChanges();
        }
    }
}
