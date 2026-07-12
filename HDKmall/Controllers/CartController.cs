using HDKmall.BLL.Interfaces;
using HDKmall.Models;
using HDKmall.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HDKmall.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IProductSearchService _productSearchService;
        private readonly IRecommendationService _recommendationService;

        public CartController(ICartService cartService, IProductSearchService productSearchService, IRecommendationService recommendationService)
        {
            _cartService = cartService;
            _productSearchService = productSearchService;
            _recommendationService = recommendationService;
        }

        // GET: Cart
        public IActionResult Index()
        {
            var userId = GetUserId();

            var cart = _cartService.GetOrCreateCart(userId, null);
            if (cart == null || !cart.Items.Any())
            {
                ViewBag.RecommendedProducts = _recommendationService.GetPersonalizedRecommendations(10);
                return View(new ShoppingCart());
            }

            var productIdsInCart = cart.Items.Select(i => i.ProductId).ToList();
            ViewBag.RecommendedProducts = _recommendationService.GetRecommendationsForCart(productIdsInCart, 10);

            return View(cart);
        }

        // POST: Cart/Add
        [HttpPost]
        [AllowAnonymous]
        public IActionResult Add(int productId, int variantId = 0, int quantity = 1)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng", requireLogin = true });
                }

                TempData["error"] = "Vui lòng đăng nhập để sử dụng giỏ hàng";
                return RedirectToAction("Login", "Account");
            }

            var cart = _cartService.GetOrCreateCart(userId, null);
            if (cart != null)
            {
                int cartItemId = _cartService.AddToCart(cart.Id, productId, variantId, quantity);
                var count = _cartService.GetCartItemCount(userId, null);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Sản phẩm đã được thêm vào giỏ hàng", cartCount = count, cartItemId = cartItemId });
                }

                TempData["success"] = "Sản phẩm đã được thêm vào giỏ hàng";
                return RedirectToAction(nameof(Index));
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "Lỗi khi thêm sản phẩm", cartCount = 0 });
            }

            TempData["error"] = "Có lỗi xảy ra khi thêm vào giỏ hàng";
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/Remove
        [HttpPost]
        public IActionResult Remove(int cartItemId)
        {
            _cartService.RemoveFromCart(cartItemId);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var userId = GetUserId();
                var count = _cartService.GetCartItemCount(userId, null);
                return Json(new { success = true, cartCount = count });
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/Update
        [HttpPost]
        public IActionResult Update(int cartItemId, int quantity)
        {
            if (quantity > 0)
            {
                _cartService.UpdateCartItem(cartItemId, quantity);
            }
            else
            {
                _cartService.RemoveFromCart(cartItemId);
            }
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var userId = GetUserId();
                var count = _cartService.GetCartItemCount(userId, null);
                return Json(new { success = true, cartCount = count });
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/Clear
        [HttpPost]
        public IActionResult Clear()
        {
            var userId = GetUserId();
            var cart = _cartService.GetOrCreateCart(userId, null);

            if (cart != null)
            {
                _cartService.ClearCart(cart.Id);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Cart/Count - returns item count as JSON (for badge update)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Count()
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Json(new { count = 0 });
            }
            var count = _cartService.GetCartItemCount(userId, null);
            return Json(new { count });
        }

        // POST: Cart/ProceedToCheckout - stores selected item IDs and redirects
        [HttpPost]
        public IActionResult ProceedToCheckout(List<int> selectedItems)
        {
            if (selectedItems == null || !selectedItems.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm để đặt hàng";
                return RedirectToAction(nameof(Index));
            }

            TempData["SelectedCartItems"] = string.Join(",", selectedItems);
            return RedirectToAction("Checkout", "Order");
        }

        private int? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim?.Value, out var userId))
            {
                return userId;
            }
            return null;
        }
    }
}

