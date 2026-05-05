using HDKmall.BLL.Interfaces;
using HDKmall.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HDKmall.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BannerController : Controller
    {
        private readonly IBannerService _bannerService;
        private readonly IPhotoService _photoService;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IBrandService _brandService;

        public BannerController(
            IBannerService bannerService, 
            IPhotoService photoService,
            IProductService productService,
            ICategoryService categoryService,
            IBrandService brandService)
        {
            _bannerService = bannerService;
            _photoService = photoService;
            _productService = productService;
            _categoryService = categoryService;
            _brandService = brandService;
        }

        public IActionResult Index(string? q)
        {
            ViewBag.ActiveTab = "banners";
            var banners = _bannerService.GetAllBanners();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim().ToLower();
                banners = banners.Where(b => (b.Title ?? "").ToLower().Contains(key));
            }

            ViewBag.Search = q;
            return View(banners);
        }

        [HttpGet]
        public IActionResult GetLinkOptions()
        {
            var products = _productService.GetAllProducts()
                .Select(p => new { id = $"/product/{p.Slug}", text = $"Sản phẩm: {p.Name}" })
                .ToList();

            var categories = _categoryService.GetAllCategories()
                .Select(c => new { id = $"/Category?categoryId={c.Id}", text = $"Danh mục: {c.Name}" })
                .ToList();

            var brands = _brandService.GetAllBrands()
                .Select(b => new { id = $"/Brand?brandId={b.Id}", text = $"Thương hiệu: {b.Name}" })
                .ToList();

            var results = new List<object>();
            results.AddRange(products);
            results.AddRange(categories);
            results.AddRange(brands);

            return Json(results);
        }

        public IActionResult Create()
        {
            ViewBag.ActiveTab = "banners";
            ViewBag.Products = _productService.GetAllProducts().ToList();
            ViewBag.Categories = _categoryService.GetAllCategories().ToList();
            ViewBag.Brands = _brandService.GetAllBrands().ToList();
            return View(new Banner { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Banner banner)
        {
            if (banner.ImageFile != null && banner.ImageFile.Length > 0)
            {
                var upload = await _photoService.AddPhotoAsync(banner.ImageFile);
                if (upload.Error == null) banner.ImageUrl = upload.SecureUrl.AbsoluteUri;
            }

            if (string.IsNullOrWhiteSpace(banner.ImageUrl))
            {
                ModelState.AddModelError("ImageUrl", "Vui lòng chọn ảnh hoặc nhập URL ảnh.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ActiveTab = "banners";
                ViewBag.Products = _productService.GetAllProducts().ToList();
                ViewBag.Categories = _categoryService.GetAllCategories().ToList();
                ViewBag.Brands = _brandService.GetAllBrands().ToList();
                return View(banner);
            }

            _bannerService.CreateBanner(banner);
            TempData["Success"] = "Đã thêm banner thành công.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            ViewBag.ActiveTab = "banners";
            var banner = _bannerService.GetBannerById(id);
            if (banner == null) return NotFound();
            ViewBag.Products = _productService.GetAllProducts().ToList();
            ViewBag.Categories = _categoryService.GetAllCategories().ToList();
            ViewBag.Brands = _brandService.GetAllBrands().ToList();
            return View(banner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Banner banner)
        {
            if (banner.ImageFile != null && banner.ImageFile.Length > 0)
            {
                var upload = await _photoService.AddPhotoAsync(banner.ImageFile);
                if (upload.Error == null) banner.ImageUrl = upload.SecureUrl.AbsoluteUri;
            }

            if (string.IsNullOrWhiteSpace(banner.ImageUrl))
            {
                ModelState.AddModelError("ImageUrl", "Vui lòng chọn ảnh hoặc nhập URL ảnh.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ActiveTab = "banners";
                ViewBag.Products = _productService.GetAllProducts().ToList();
                ViewBag.Categories = _categoryService.GetAllCategories().ToList();
                ViewBag.Brands = _brandService.GetAllBrands().ToList();
                return View(banner);
            }

            _bannerService.UpdateBanner(id, banner);
            TempData["Success"] = "Đã cập nhật banner.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            _bannerService.DeleteBanner(id);
            TempData["Success"] = "Đã xóa banner.";
            return RedirectToAction(nameof(Index));
        }
    }
}