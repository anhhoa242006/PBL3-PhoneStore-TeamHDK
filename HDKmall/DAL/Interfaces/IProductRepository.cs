using HDKmall.Models;

namespace HDKmall.DAL.Interfaces
{
    public interface IProductRepository
    {
        IEnumerable<Product> GetAll();
        Product GetById(int id);
        Product GetBySlug(string slug);
        void Add(Product product);
        void Update(Product product);
        void Delete(int id);

        // Version methods
        void AddVersion(ProductVersion version);
        void UpdateVersion(ProductVersion version);
        void DeleteVersions(int productId);
        void DeleteVersionById(int versionId);

        // Image methods
        void AddImage(ProductImage image);
        void DeleteImage(int imageId);

        // Variant methods
        void AddVariant(ProductVariant variant);
        void UpdateVariant(ProductVariant variant);
        void DeleteVariants(int productId);
        void DeleteVariantById(int variantId);
        void DeleteVariantsByVersionId(int versionId);

        // Specification methods
        void AddSpecification(ProductSpecification spec);
        void UpdateSpecification(ProductSpecification spec);
        void DeleteSpecifications(int productId);
        void DeleteSpecById(int specId);
        void DeleteSpecificationsByVersionId(int versionId);
        void SaveChanges();
    }
}