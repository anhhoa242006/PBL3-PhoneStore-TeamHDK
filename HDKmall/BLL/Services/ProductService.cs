using HDKmall.BLL.Interfaces;
using HDKmall.DAL.Interfaces;
using HDKmall.Models;
using HDKmall.ViewModels;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HDKmall.BLL.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IPhotoService _photoService;

        public ProductService(IProductRepository productRepository, IPhotoService photoService)
        {
            _productRepository = productRepository;
            _photoService = photoService;
        }

        public IEnumerable<Product> GetAllProducts()
        {
            return _productRepository.GetAll();
        }

        public Product GetProductById(int id)
        {
            return _productRepository.GetById(id);
        }

        public async Task<bool> AddProductAsync(ProductVM vm)
        {
            // Tính giá lưu vào Product tuỳ theo loại
            decimal productPrice = vm.ProductType == ProductType.HasVersions
                ? (vm.Versions?.Where(v => v.BasePrice > 0).Min(v => (decimal?)v.BasePrice) ?? 0)
                : vm.Price;

            var product = new Product
            {
                Name = vm.Name,
                Price = productPrice,
                OriginalPrice = vm.OriginalPrice,
                Description = vm.Description,
                CategoryId = vm.CategoryId,
                BrandId = vm.BrandId,
                ProductType = (int)vm.ProductType // Ép kiểu sang int
            };

            // Handle main product image
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                var result = await _photoService.AddPhotoAsync(vm.ImageFile);
                if (result.Error == null)
                    product.ImageUrl = result.SecureUrl.AbsoluteUri;
            }

            _productRepository.Add(product);

            if (vm.ProductType == ProductType.HasVersions && vm.Versions != null && vm.Versions.Count > 0)
            {
                bool firstVersion = true;
                foreach (var vVM in vm.Versions)
                {
                    if (string.IsNullOrWhiteSpace(vVM.Name)) continue;

                    var version = new ProductVersion
                    {
                        ProductId = product.Id,
                        Name = vVM.Name,
                        BasePrice = vVM.BasePrice,
                        OriginalPrice = vVM.OriginalPrice,
                        Description = vVM.Description
                    };

                    // Handle version main image
                    if (vVM.ImageFile != null && vVM.ImageFile.Length > 0)
                    {
                        var result = await _photoService.AddPhotoAsync(vVM.ImageFile);
                        if (result.Error == null)
                        {
                            version.ImageUrl = result.SecureUrl.AbsoluteUri;
                            // Use first version's image as product thumbnail if product doesn't have one
                            if (firstVersion && string.IsNullOrEmpty(product.ImageUrl))
                            {
                                product.ImageUrl = version.ImageUrl;
                                _productRepository.Update(product);
                            }
                        }
                    }

                    _productRepository.AddVersion(version);
                    firstVersion = false;

                    // Handle variants (Colors) for this version
                    if (vVM.Variants != null)
                    {
                        foreach (var varVM in vVM.Variants)
                        {
                            if (string.IsNullOrWhiteSpace(varVM.Color)) continue;

                            string? variantImageUrl = null;
                            if (varVM.ImageFile != null && varVM.ImageFile.Length > 0)
                            {
                                var upload = await _photoService.AddPhotoAsync(varVM.ImageFile);
                                if (upload.Error == null)
                                    variantImageUrl = upload.SecureUrl.AbsoluteUri;
                            }

                            var variant = new ProductVariant
                            {
                                ProductVersionId = version.Id,
                                Color = varVM.Color,
                                Price = varVM.Price > 0 ? varVM.Price : version.BasePrice,
                                Stock = varVM.Stock,
                                ImageUrl = variantImageUrl
                            };
                            _productRepository.AddVariant(variant);
                        }
                    }

                    // Handle specifications for this version
                    if (vVM.Specifications != null)
                    {
                        int order = 0;
                        foreach (var sVM in vVM.Specifications)
                        {
                            if (string.IsNullOrWhiteSpace(sVM.SpecName)) continue;
                            var spec = new ProductSpecification
                            {
                                ProductVersionId = version.Id,
                                SpecName = sVM.SpecName,
                                SpecValue = sVM.SpecValue ?? "",
                                DisplayOrder = sVM.DisplayOrder > 0 ? sVM.DisplayOrder : order++
                            };
                            _productRepository.AddSpecification(spec);
                        }
                    }
                }
            }
            else if (vm.ProductType == ProductType.ColorsOnly && vm.Versions != null && vm.Versions.Count > 0)
            {
                // Tạo Version "Mặc định" ẩn để gắn màu sắc vào
                var defaultVersion = new ProductVersion
                {
                    ProductId = product.Id,
                    Name = "Mặc định",
                    BasePrice = vm.Price,
                    OriginalPrice = vm.OriginalPrice
                };
                _productRepository.AddVersion(defaultVersion);

                var colorVM = vm.Versions[0];
                if (colorVM.Variants != null)
                {
                    foreach (var varVM in colorVM.Variants)
                    {
                        if (string.IsNullOrWhiteSpace(varVM.Color)) continue;
                        string? variantImageUrl = null;
                        if (varVM.ImageFile != null && varVM.ImageFile.Length > 0)
                        {
                            var upload = await _photoService.AddPhotoAsync(varVM.ImageFile);
                            if (upload.Error == null) variantImageUrl = upload.SecureUrl.AbsoluteUri;
                        }
                        _productRepository.AddVariant(new ProductVariant
                        {
                            ProductVersionId = defaultVersion.Id,
                            Color = varVM.Color,
                            Price = varVM.Price > 0 ? varVM.Price : vm.Price,
                            Stock = varVM.Stock,
                            ImageUrl = variantImageUrl
                        });
                    }
                }

                // Sao chép thông số kỹ thuật cho sản phẩm chỉ có màu sắc
                if (colorVM.Specifications != null)
                {
                    int order = 0;
                    foreach (var sVM in colorVM.Specifications)
                    {
                        if (string.IsNullOrWhiteSpace(sVM.SpecName)) continue;
                        var spec = new ProductSpecification
                        {
                            ProductVersionId = defaultVersion.Id,
                            SpecName = sVM.SpecName,
                            SpecValue = sVM.SpecValue ?? "",
                            DisplayOrder = sVM.DisplayOrder > 0 ? sVM.DisplayOrder : order++
                        };
                        _productRepository.AddSpecification(spec);
                    }
                }
            }
            // Mặc định: chỉ Product.Price, không cần Version/Variant (ví dụ cho các loại khác nếu có)

            if (vm.GalleryFiles != null)
            {
                int order = 0;
                foreach (var file in vm.GalleryFiles)
                {
                    if (file == null || file.Length == 0) continue;
                    var result = await _photoService.AddPhotoAsync(file);
                    if (result.Error != null) continue;

                    var img = new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = result.SecureUrl.AbsoluteUri,
                        PublicId = result.PublicId,
                        IsMain = false,
                        DisplayOrder = order++
                    };
                    _productRepository.AddImage(img);
                }
            }

            return true;
        }

        public async Task<bool> UpdateProductAsync(int id, ProductVM vm)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return false;

            product.Name = vm.Name;
            product.Price = vm.Price;
            product.OriginalPrice = vm.OriginalPrice; // Lưu giá gốc
            product.ProductType = (int)vm.ProductType; // Ép kiểu sang int
            product.Description = vm.Description;
            product.CategoryId = vm.CategoryId;
            product.BrandId = vm.BrandId;
            product.IsActive = vm.IsActive;

            // If product is set to inactive, immediately reset all variant stock to 0
            if (!vm.IsActive)
            {
                var allVariants = product.Versions
                    .SelectMany(v => v.Variants ?? new List<ProductVariant>())
                    .ToList();
                foreach (var vr in allVariants)
                {
                    vr.Stock = 0;
                    _productRepository.UpdateVariant(vr);
                }
            }

            // Handle main product image update
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                var result = await _photoService.AddPhotoAsync(vm.ImageFile);
                if (result.Error == null)
                    product.ImageUrl = result.SecureUrl.AbsoluteUri;
            }
            else
            {
                // If vm.ImageUrl is null or empty, it means the user deleted it
                product.ImageUrl = string.IsNullOrEmpty(vm.ImageUrl) ? null : vm.ImageUrl;
            }

            _productRepository.Update(product);

            // --- SMART VERSION UPDATE (Non-destructive) ---
            var existingVersions = product.Versions.ToList();
            var submittedVersionIds = vm.Versions?.Where(v => v.Id > 0).Select(v => v.Id).ToList() ?? new List<int>();

            // 1. Delete versions removed from UI (only if no reviews to prevent crash)
            foreach (var existingV in existingVersions)
            {
                if (!submittedVersionIds.Contains(existingV.Id))
                {
                    // Check reviews - if it has reviews, we cannot simply delete it 
                    // without migrating reviews. For now, we skip deletion to prevent DB error.
                    if (existingV.Reviews == null || !existingV.Reviews.Any())
                    {
                        _productRepository.DeleteVariantsByVersionId(existingV.Id);
                        _productRepository.DeleteSpecificationsByVersionId(existingV.Id);
                        _productRepository.DeleteVersionById(existingV.Id);
                    }
                }
            }

            // 2. Update existing or Add new versions
            if (vm.Versions != null)
            {
                for (int v = 0; v < vm.Versions.Count; v++)
                {
                    var vVM = vm.Versions[v];
                    if (string.IsNullOrWhiteSpace(vVM.Name)) continue;

                    ProductVersion version;
                    bool isNewVersion = false;

                    if (vVM.Id > 0)
                    {
                        version = existingVersions.FirstOrDefault(ex => ex.Id == vVM.Id);
                        if (version == null) // Fallback if ID not found
                        {
                            version = new ProductVersion { ProductId = product.Id };
                            isNewVersion = true;
                        }
                        else
                        {
                            version.Name = vVM.Name;
                            // Nếu ColorsOnly, dùng giá của Product cho Version mặc định
                            version.BasePrice = (vm.ProductType == ProductType.ColorsOnly) ? vm.Price : vVM.BasePrice;
                            version.OriginalPrice = (vm.ProductType == ProductType.ColorsOnly) ? vm.OriginalPrice : vVM.OriginalPrice;
                            version.Description = vVM.Description;
                        }
                    }
                    else
                    {
                        version = new ProductVersion { ProductId = product.Id, Name = vVM.Name, BasePrice = vVM.BasePrice, OriginalPrice = vVM.OriginalPrice, Description = vVM.Description };
                        isNewVersion = true;
                    }

                    // Handle Version Image
                    if (vVM.ImageFile != null && vVM.ImageFile.Length > 0)
                    {
                        var result = await _photoService.AddPhotoAsync(vVM.ImageFile);
                        if (result.Error == null)
                        {
                            version.ImageUrl = result.SecureUrl.AbsoluteUri;
                            if (v == 0) product.ImageUrl = version.ImageUrl; // Update main thumb if first version
                        }
                    }
                    else
                    {
                        version.ImageUrl = string.IsNullOrEmpty(vVM.ImageUrl) ? null : vVM.ImageUrl;
                    }

                    if (isNewVersion) _productRepository.AddVersion(version);
                    else _productRepository.UpdateVersion(version);

                    // --- Smart Variant Sync for this version ---
                    var existingVariants = version.Variants?.ToList() ?? new List<ProductVariant>();
                    var submittedVariantIds = vVM.Variants?.Where(vr => vr.Id > 0).Select(vr => vr.Id).ToList() ?? new List<int>();

                    // Delete removed variants
                    foreach (var exVr in existingVariants)
                    {
                        if (!submittedVariantIds.Contains(exVr.Id)) _productRepository.DeleteVariantById(exVr.Id);
                    }

                    // Update/Add variants
                    if (vVM.Variants != null)
                    {
                        foreach (var vrVM in vVM.Variants)
                        {
                            if (string.IsNullOrWhiteSpace(vrVM.Color)) continue;
                            ProductVariant variant;
                            bool isNewVariant = false;

                            if (vrVM.Id > 0)
                            {
                                variant = existingVariants.FirstOrDefault(ex => ex.Id == vrVM.Id);
                                if (variant == null) { variant = new ProductVariant { ProductVersionId = version.Id }; isNewVariant = true; }
                                else { 
                                    variant.Color = vrVM.Color; 
                                    variant.Price = vrVM.Price > 0 ? vrVM.Price : version.BasePrice; 
                                    variant.Stock = vrVM.Stock; 
                                }
                            }
                            else
                            {
                                variant = new ProductVariant { ProductVersionId = version.Id, Color = vrVM.Color, Price = vrVM.Price > 0 ? vrVM.Price : version.BasePrice, Stock = vrVM.Stock };
                                isNewVariant = true;
                            }

                            if (vrVM.ImageFile != null && vrVM.ImageFile.Length > 0)
                            {
                                var upload = await _photoService.AddPhotoAsync(vrVM.ImageFile);
                                if (upload.Error == null) variant.ImageUrl = upload.SecureUrl.AbsoluteUri;
                            }
                            else
                            {
                                variant.ImageUrl = string.IsNullOrEmpty(vrVM.ImageUrl) ? null : vrVM.ImageUrl;
                            }

                            if (isNewVariant) _productRepository.AddVariant(variant);
                            else _productRepository.UpdateVariant(variant);
                        }
                    }

                    // --- Smart Specifications Sync ---
                    var existingSpecs = version.Specifications?.ToList() ?? new List<ProductSpecification>();
                    var submittedSpecIds = vVM.Specifications?.Where(s => s.Id > 0).Select(s => s.Id).ToList() ?? new List<int>();

                    foreach (var exSpec in existingSpecs)
                    {
                        if (!submittedSpecIds.Contains(exSpec.Id)) _productRepository.DeleteSpecById(exSpec.Id);
                    }

                    if (vVM.Specifications != null)
                    {
                        int order = 0;
                        foreach (var sVM in vVM.Specifications)
                        {
                            if (string.IsNullOrWhiteSpace(sVM.SpecName)) continue;
                            ProductSpecification spec;
                            bool isNewSpec = false;

                            if (sVM.Id > 0)
                            {
                                spec = existingSpecs.FirstOrDefault(ex => ex.Id == sVM.Id);
                                if (spec == null) { spec = new ProductSpecification { ProductVersionId = version.Id }; isNewSpec = true; }
                                else { spec.SpecName = sVM.SpecName; spec.SpecValue = sVM.SpecValue ?? ""; spec.DisplayOrder = sVM.DisplayOrder > 0 ? sVM.DisplayOrder : order++; }
                            }
                            else
                            {
                                spec = new ProductSpecification { ProductVersionId = version.Id, SpecName = sVM.SpecName, SpecValue = sVM.SpecValue ?? "", DisplayOrder = sVM.DisplayOrder > 0 ? sVM.DisplayOrder : order++ };
                                isNewSpec = true;
                            }

                            if (isNewSpec) _productRepository.AddSpecification(spec);
                            else _productRepository.UpdateSpecification(spec);
                        }
                    }
                }
            }

            // Handle Gallery Deletion
            if (vm.ExistingImages != null)
            {
                foreach (var exImg in vm.ExistingImages)
                {
                    if (exImg.IsDeleted) // Use the new IsDeleted flag
                    {
                        if (!string.IsNullOrEmpty(exImg.PublicId))
                            await _photoService.DeletePhotoAsync(exImg.PublicId);
                        
                        _productRepository.DeleteImage(exImg.Id);
                    }
                }
            }

            // Handle New Gallery Images
            if (vm.GalleryFiles != null)
            {
                int order = 0;
                foreach (var file in vm.GalleryFiles)
                {
                    if (file == null || file.Length == 0) continue;
                    var result = await _photoService.AddPhotoAsync(file);
                    if (result.Error != null) continue;

                    var img = new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = result.SecureUrl.AbsoluteUri,
                        PublicId = result.PublicId,
                        IsMain = false,
                        DisplayOrder = order++
                    };
                    _productRepository.AddImage(img);
                }
            }

            return true;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = _productRepository.GetById(id);
                if (product == null) return false;

                // Delete images from cloud (simplified)
                // ... (optional logic for cleanup)

                _productRepository.Delete(id);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}