using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;
using HDKmall.BLL.Interfaces;
using HDKmall.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HDKmall.Models;
using System.Security.Claims;
using System.Linq;

namespace HDKmall.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductSearchService _searchService;
        private readonly ICategoryService _categoryService;
        private readonly IBrandService _brandService;
        private readonly IReviewService _reviewService;
        private readonly HDKmall.DAL.Interfaces.IProductRepository _productRepo;
        private readonly IRecommendationService _recommendationService;
        private readonly IWishlistService _wishlistService;
        private readonly ApplicationDbContext _context;

        public ProductController(IProductSearchService searchService, ICategoryService categoryService, IBrandService brandService, IReviewService reviewService, HDKmall.DAL.Interfaces.IProductRepository productRepo, IRecommendationService recommendationService, IWishlistService wishlistService, ApplicationDbContext context)
        {
            _searchService = searchService;
            _categoryService = categoryService;
            _brandService = brandService;
            _reviewService = reviewService;
            _productRepo = productRepo;
            _recommendationService = recommendationService;
            _wishlistService = wishlistService;
            _context = context;
        }

        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            text = text.Normalize(NormalizationForm.FormD);
            var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC).Replace("đ", "d").Replace("Đ", "D");
        }

        // GET: Product/Index
        public async Task<IActionResult> Index(int? categoryId, string cat, int? brandId, string brand, decimal? minPrice, decimal? maxPrice, string q, string sortBy = "newest", int page = 1)
        {
            var allCategories = _categoryService.GetAllCategories();
            var allBrands = _brandService.GetAllBrands();

            if (!string.IsNullOrEmpty(cat) && !categoryId.HasValue)
            {
                var category = allCategories.FirstOrDefault(c => RemoveDiacritics(c.Name).ToLower().Replace(" ", "-") == cat.ToLower());
                if (category != null)
                {
                    categoryId = category.Id;
                }
                else
                {
                    categoryId = -1; // Force no results if category slug is invalid
                }
            }

            if (!string.IsNullOrEmpty(brand) && !brandId.HasValue)
            {
                var br = allBrands.FirstOrDefault(b => RemoveDiacritics(b.Name).ToLower().Replace(" ", "-") == brand.ToLower());
                if (br != null)
                {
                    brandId = br.Id;
                }
                else
                {
                    brandId = -1; // Force no results if brand slug is invalid
                }
            }

            var filter = new ProductFilterVM
            {
                SearchQuery = q,
                CategoryId = categoryId,
                BrandId = brandId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy,
                PageNumber = page,
                PageSize = 12
            };

            var result = _searchService.SearchProducts(filter);

            // Pass filter info to view for sidebar
            ViewBag.Categories = allCategories;
            ViewBag.Brands = allBrands;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedBrandId = brandId;
            ViewBag.SelectedMinPrice = minPrice;
            ViewBag.SelectedMaxPrice = maxPrice;
            ViewBag.SelectedSortBy = sortBy;
            ViewBag.SearchQuery = q;

            // Track search history
            if (!string.IsNullOrEmpty(q))
            {
                _recommendationService.AddToRecentSearches(q);
            }

            if (User.Identity.IsAuthenticated)
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    var wishlist = await _wishlistService.GetUserWishlistAsync(userId);
                    ViewBag.WishlistProductIds = wishlist.Select(w => w.Id).ToList();
                }
            }

            return View(result);
        }

        // GET: Product/Detail/5 or /product/slug
        [Route("Product/Detail/{id:int}")]
        [Route("product/{slug}")]
        public async Task<IActionResult> Detail(int? id, string slug, int? versionId)
        {
            ProductDetailVM product = null;

            if (!string.IsNullOrEmpty(slug))
            {
                product = _searchService.GetProductDetailBySlug(slug);
                
                // Fallback: Nếu không tìm thấy theo slug trong DB (do chưa update dữ liệu cũ)
                if (product == null)
                {
                    var allProducts = _productRepo.GetAll();
                    var match = allProducts.FirstOrDefault(p => HDKmall.Helpers.SlugHelper.GenerateSlug(p.Name) == slug);
                    if (match != null)
                    {
                        match.Slug = slug;
                        _productRepo.Update(match);
                        product = _searchService.GetProductDetail(match.Id);
                    }
                }
            }
            else if (id.HasValue)
            {
                product = _searchService.GetProductDetail(id.Value);
                // Tự động cập nhật slug nếu còn thiếu để lần sau link đẹp hơn
                if (product != null && string.IsNullOrEmpty(product.Slug))
                {
                    var p = _productRepo.GetById(product.Id);
                    if (p != null)
                    {
                        p.Slug = HDKmall.Helpers.SlugHelper.GenerateSlug(p.Name);
                        _productRepo.Update(p);
                    }
                }
            }

            if (product == null)
            {
                return NotFound();
            }

            // Track recently viewed
            _recommendationService.AddToRecentlyViewed(product.Id);

            // Get related products
            ViewBag.RelatedProducts = _recommendationService.GetRelatedProducts(product.Id);

            // Identify the active version ID
            var activeVersionId = versionId ?? product.Versions.FirstOrDefault()?.Id ?? 0;
            ViewBag.ActiveVersionId = activeVersionId;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId) && userId > 0)
                {
                    // Load wishlist for heart-icon state
                    var wishlist = await _wishlistService.GetUserWishlistAsync(userId);
                    ViewBag.WishlistProductIds = wishlist.Select(w => w.Id).ToList();

                    // Check if user bought THIS SPECIFIC version
                    var hasBoughtActive = _reviewService.HasUserBoughtVersion(userId, activeVersionId);
                    ViewBag.HasBought = hasBoughtActive;

                    // Get user's existing review for this specific version
                    var existingReview = _reviewService.GetUserReviewForVersion(userId, activeVersionId);
                    ViewBag.UserExistingReview = existingReview;

                    // Can review if bought AND (no existing review OR existing review is not edited yet)
                    ViewBag.CanReview = hasBoughtActive && (existingReview == null || !existingReview.IsEdited);

                    ViewBag.DefaultReviewVersionId = activeVersionId;
                }
                else
                {
                    ViewBag.HasBought = false;
                    ViewBag.CanReview = false;
                    ViewBag.UserExistingReview = null;
                }
            }
            else
            {
                ViewBag.HasBought = false;
                ViewBag.CanReview = false;
                ViewBag.UserExistingReview = null;
            }

            ViewBag.RequestedVersionId = versionId;

            // Filter reviews to be version-specific (per user request)
            if (activeVersionId > 0)
            {
                // We keep a backup of all reviews if needed, but for display we filter
                var versionReviews = product.Reviews.Where(r => r.ProductVersionId == activeVersionId).ToList();
                product.Reviews = versionReviews;
                product.TotalReviews = versionReviews.Count;
                product.AverageRating = versionReviews.Any() ? (double)versionReviews.Average(r => r.Rating) : 0;
            }

            return View(product);
        }

        // GET: Product/Search?q=iphone
        [HttpGet("Product/Search")]
        public IActionResult Search(string q, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index", new { q = q, page = page });
        }

        [HttpGet("Product/Promotions")]
        public async Task<IActionResult> Promotions(int? categoryId, string sortBy = "newest", int page = 1)
        {
            var filter = new ProductFilterVM
            {
                OnlyPromotions = true,
                CategoryId = categoryId,
                SortBy = sortBy,
                PageNumber = page,
                PageSize = 12
            };

            var result = _searchService.SearchProducts(filter);
            ViewBag.SelectedSortBy = sortBy;

            // Get categories for Tabs
            ViewBag.Categories = _categoryService.GetAllCategories();
            ViewBag.SelectedCategoryId = categoryId;
            
            // Get active promotion banner
            var activePromotion = _context.Promotions.FirstOrDefault(p => p.IsActive && p.StartDate <= HDKmall.Helpers.TimeHelper.GetVietnamTime() && p.EndDate >= HDKmall.Helpers.TimeHelper.GetVietnamTime());
            ViewBag.ActivePromotion = activePromotion;

            if (User.Identity.IsAuthenticated)
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    var wishlist = await _wishlistService.GetUserWishlistAsync(userId);
                    ViewBag.WishlistProductIds = wishlist.Select(w => w.Id).ToList();
                }
            }

            return View(result);
        }

        // GET: Product/Category/1
        [HttpGet("Product/Category/{id:int}")]
        public IActionResult Category(int id, int page = 1)
        {
            var allCategories = _categoryService.GetAllCategories();
            var allBrands = _brandService.GetAllBrands();

            var filter = new ProductFilterVM
            {
                CategoryId = id,
                PageNumber = page,
                PageSize = 12
            };

            var result = _searchService.SearchProducts(filter);
            ViewBag.Categories = allCategories;
            ViewBag.Brands = allBrands;
            ViewBag.SelectedCategoryId = id;

            return View("Index", result);
        }

        // GET: Product/Brand/1
        [HttpGet("Product/Brand/{id:int}")]
        public IActionResult Brand(int id, int page = 1)
        {
            var allCategories = _categoryService.GetAllCategories();
            var allBrands = _brandService.GetAllBrands();

            var filter = new ProductFilterVM
            {
                BrandId = id,
                PageNumber = page,
                PageSize = 12
            };

            var result = _searchService.SearchProducts(filter);
            ViewBag.Categories = allCategories;
            ViewBag.Brands = allBrands;
            ViewBag.SelectedBrandId = id;

            return View("Index", result);
        }

        // API Endpoints for Home Page
        [HttpGet]
        public IActionResult GetCategories()
        {
            try
            {
                var categories = _categoryService.GetAllCategories();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetBrands()
        {
            try
            {
                var brands = _brandService.GetAllBrands();
                var brandNames = brands.Select(b => b.Name).ToList();
                return Ok(brandNames);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetProducts()
        {
            try
            {
                var filter = new ProductFilterVM { PageSize = 100 };
                var result = _searchService.SearchProducts(filter);
                return Ok(result.Products);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("Product/UpdateAllSlugs")]
        public IActionResult UpdateAllSlugs()
        {
            var products = _productRepo.GetAll();
            int count = 0;
            foreach (var p in products)
            {
                if (string.IsNullOrEmpty(p.Slug))
                {
                    p.Slug = HDKmall.Helpers.SlugHelper.GenerateSlug(p.Name);
                    _productRepo.Update(p);
                    count++;
                }
            }
            TempData["success"] = $"Đã cập nhật thành công {count} sản phẩm!";
            return RedirectToAction("Index");
        }
        
        [HttpGet]
        public IActionResult GetRecentSearches()
        {
            return Ok(_recommendationService.GetRecentSearches());
        }

        [HttpGet]
        public IActionResult GetPersonalizedRecommendations(int take = 10)
        {
            return Ok(_recommendationService.GetPersonalizedRecommendations(take));
        }
    }
}
