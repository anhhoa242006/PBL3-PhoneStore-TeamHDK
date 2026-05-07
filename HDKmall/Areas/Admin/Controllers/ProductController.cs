using HDKmall.BLL.Interfaces;
using HDKmall.Models;
using HDKmall.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;

namespace HDKmall.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IBrandService _brandService;

        public ProductController(IProductService productService, ICategoryService categoryService, IBrandService brandService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _brandService = brandService;
        }

        private void PopulateDropdowns(int? selectedCategoryId = null, int? selectedBrandId = null)
        {
            var categories = _categoryService.GetAllCategories();
            var brands = _brandService.GetAllBrands();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedCategoryId);
            ViewBag.Brands = new SelectList(brands, "Id", "Name", selectedBrandId);
        }

        public IActionResult Index(string? q)
        {
            ViewBag.ActiveTab = "products";
            var productsQuery = _productService.GetAllProducts().ToList();

            var productsVM = productsQuery.Select(p => {
                // Calculate a display price: for versioned products, use the minimum price among versions.
                // If no versions or standard product, use the base Price.
                decimal displayPrice = p.Price;
                if (p.ProductType == 1 && p.Versions != null && p.Versions.Any())
                {
                    var validPrices = p.Versions.Where(v => v.BasePrice > 0).Select(v => v.BasePrice).ToList();
                    if (validPrices.Any()) 
                    {
                        displayPrice = validPrices.Min();
                    }
                }

                return new ProductListVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = displayPrice,
                    IsActive = p.IsActive,
                    ImageUrl = p.ImageUrl,
                    CategoryName = p.Category?.Name ?? p.CategoryId.ToString(),
                    BrandName = p.Brand?.Name,
                    Slug = p.Slug
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim().ToLower();
                productsVM = productsVM.Where(p =>
                    (p.Name ?? "").ToLower().Contains(key) ||
                    (p.CategoryName ?? "").ToLower().Contains(key) ||
                    (p.BrandName ?? "").ToLower().Contains(key)
                ).ToList();
            }

            ViewBag.Search = q;
            return View(productsVM);
        }

        public IActionResult Create()
        {
            ViewBag.ActiveTab = "products";
            PopulateDropdowns();
            return View(new ProductVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductVM vm)
        {
            if (ModelState.IsValid)
            {
                var result = await _productService.AddProductAsync(vm);
                if (result)
                {
                    TempData["Success"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Lỗi khi lưu sản phẩm. Vui lòng kiểm tra lại.");
            }
            else
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                ModelState.AddModelError("", "Lỗi nhập liệu: " + errors);
            }
            ViewBag.ActiveTab = "products";
            PopulateDropdowns(vm.CategoryId, vm.BrandId);
            return View(vm);
        }

        public IActionResult Edit(int id)
        {
            ViewBag.ActiveTab = "products";
            var product = _productService.GetProductById(id);
            if (product == null) return NotFound();

            var vm = new ProductVM
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                ProductType = (ProductType)product.ProductType,
                IsActive = product.IsActive,
                ImageUrl = product.ImageUrl,
                ExistingImages = (product.Images ?? new List<ProductImage>())
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ProductImageVM 
                    { 
                        Id = i.Id, 
                        ImageUrl = i.ImageUrl,
                        PublicId = i.PublicId,
                        DisplayOrder = i.DisplayOrder,
                        IsMain = false
                    }).ToList(),
                Versions = (product.Versions ?? new List<ProductVersion>()).Select(v => new ProductVersionVM
                {
                    Id = v.Id,
                    Name = v.Name,
                    BasePrice = v.BasePrice,
                    OriginalPrice = v.OriginalPrice,
                    Description = v.Description,
                    ImageUrl = v.ImageUrl,
                    Variants = (v.Variants ?? new List<ProductVariant>()).Select(vr => new ProductVariantVM
                    {
                        Id = vr.Id,
                        ProductVersionId = vr.ProductVersionId,
                        Color = vr.Color,
                        Price = vr.Price,
                        Stock = vr.Stock,
                        ImageUrl = vr.ImageUrl
                    }).ToList(),
                    Specifications = (v.Specifications ?? new List<ProductSpecification>())
                        .OrderBy(s => s.DisplayOrder)
                        .Select(s => new ProductSpecVM
                        {
                            Id = s.Id,
                            SpecName = s.SpecName,
                            SpecValue = s.SpecValue,
                            DisplayOrder = s.DisplayOrder
                        }).ToList()
                }).ToList()
            };

            PopulateDropdowns(vm.CategoryId, vm.BrandId);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductVM vm)
        {
            if (ModelState.IsValid)
            {
                var result = await _productService.UpdateProductAsync(id, vm);
                if (result)
                {
                    TempData["Success"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật sản phẩm!");
            }
            else
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                ModelState.AddModelError("", "Lỗi nhập liệu: " + errors);
            }
            ViewBag.ActiveTab = "products";
            PopulateDropdowns(vm.CategoryId, vm.BrandId);
            return View(vm);
        }

        public IActionResult Delete(int id)
        {
            ViewBag.ActiveTab = "products";
            var product = _productService.GetProductById(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _productService.DeleteProductAsync(id);
            if (result)
                TempData["Success"] = "Đã xóa sản phẩm thành công!";
            else
                TempData["Error"] = "Có lỗi xảy ra khi xóa sản phẩm!";

            return RedirectToAction(nameof(Index));
        }
    }
}