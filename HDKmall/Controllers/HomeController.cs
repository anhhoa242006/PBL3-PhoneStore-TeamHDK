using HDKmall.BLL.Interfaces;
using HDKmall.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HDKmall.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductSearchService _searchService;
        private readonly ICategoryService _categoryService;
        private readonly IBrandService _brandService;
        private readonly IRecommendationService _recommendationService;
        private readonly IBannerService _bannerService;
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(IProductSearchService searchService, ICategoryService categoryService, IBrandService brandService, IRecommendationService recommendationService, IBannerService bannerService, ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _searchService = searchService;
            _categoryService = categoryService;
            _brandService = brandService;
            _recommendationService = recommendationService;
            _bannerService = bannerService;
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var featuredProducts = _searchService.GetFeaturedProducts(8);
            var newProducts = _searchService.GetNewProducts(8);
            
            // Get Recommendations
            var recentlyViewed = _recommendationService.GetRecentlyViewedProducts(8);
            var personalized = _recommendationService.GetPersonalizedRecommendations(8);

            ViewBag.Categories = _categoryService.GetAllCategories();
            ViewBag.Brands = _brandService.GetAllBrands();
            ViewBag.FeaturedProducts = featuredProducts;
            ViewBag.NewProducts = newProducts;
            ViewBag.RecentlyViewed = recentlyViewed;
            ViewBag.PersonalizedRecommendations = personalized;
            ViewBag.Banners = _bannerService.GetAllBanners().Where(b => b.IsActive).ToList();

            if (User.Identity.IsAuthenticated)
            {
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    var wishlistService = HttpContext.RequestServices.GetService<IWishlistService>();
                    if (wishlistService != null)
                    {
                        var wishlist = await wishlistService.GetUserWishlistAsync(userId);
                        ViewBag.WishlistProductIds = wishlist.Select(w => w.Id).ToList();
                    }
                }
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            if (statusCode == 404)
            {
                ViewData["ErrorTitle"] = "404 - Không tìm thấy trang";
                ViewData["ErrorMessage"] = "Rất tiếc, trang bạn đang tìm kiếm không tồn tại hoặc đã bị di chuyển.";
            }
            else
            {
                ViewData["ErrorTitle"] = "Đã có lỗi xảy ra";
                ViewData["ErrorMessage"] = "Hệ thống đang gặp sự cố tạm thời. Vui lòng thử lại sau.";
            }

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
