using HDKmall.BLL.Interfaces;
using HDKmall.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;

namespace HDKmall.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly ICouponService _couponService;

        public OrderController(IOrderService orderService, ICartService cartService, ICouponService couponService)
        {
            _orderService = orderService;
            _cartService = cartService;
            _couponService = couponService;
        }

        // GET: Order/Checkout
        public IActionResult Checkout()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Account");

            var cart = _cartService.GetOrCreateCart(userId, null);
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng trống";
                return RedirectToAction("Index", "Cart");
            }

            // Get selected item IDs from TempData (set by Cart/ProceedToCheckout)
            List<int> selectedIds = null;
            if (TempData["SelectedCartItems"] is string selectedStr && !string.IsNullOrEmpty(selectedStr))
            {
                selectedIds = selectedStr.Split(',').Select(int.Parse).ToList();
                // Keep TempData for the POST
                TempData.Keep("SelectedCartItems");
            }

            // Filter to selected items, or use all items if none selected
            ShoppingCart checkoutCart;
            if (selectedIds != null && selectedIds.Any())
            {
                checkoutCart = new ShoppingCart
                {
                    Id = cart.Id,
                    UserId = cart.UserId,
                    // Lọc chỉ lấy những sản phẩm còn đang bán (IsActive = true)
                    Items = cart.Items.Where(i => selectedIds.Contains(i.Id) && (i.Product?.IsActive ?? true)).ToList()
                };
            }
            else
            {
                // Nếu không chọn cụ thể, lấy tất cả sản phẩm trong giỏ nhưng phải còn đang bán (IsActive)
                checkoutCart = new ShoppingCart
                {
                    Id = cart.Id,
                    UserId = cart.UserId,
                    Items = cart.Items.Where(i => i.Product?.IsActive ?? true).ToList()
                };
            }

            return View(checkoutCart);
        }

        // POST: Order/CreateOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateOrder(string address, string cityName, string districtName, string paymentMethod, string? couponCode = null, string? note = null, decimal shippingFee = 0)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            var cart = _cartService.GetOrCreateCart(userId, null);
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng trống";
                return RedirectToAction("Index", "Cart");
            }

            // Get selected item IDs
            List<int> selectedIds = null;
            if (TempData["SelectedCartItems"] is string selectedStr && !string.IsNullOrEmpty(selectedStr))
            {
                selectedIds = selectedStr.Split(',').Select(int.Parse).ToList();
            }

            var itemsToOrder = (selectedIds != null && selectedIds.Any()
                ? cart.Items.Where(i => selectedIds.Contains(i.Id))
                : cart.Items)
                .Where(i => i.Product != null && i.Product.IsActive) // Đảm bảo chỉ đặt hàng sản phẩm đang bán
                .ToList();

            if (!itemsToOrder.Any())
            {
                TempData["Error"] = "Không có sản phẩm nào được chọn";
                return RedirectToAction("Index", "Cart");
            }

            decimal discountAmount = 0;
            if (!string.IsNullOrEmpty(couponCode))
            {
                var coupon = _couponService.ValidateCoupon(couponCode);
                if (coupon != null)
                {
                    discountAmount = coupon.DiscountAmount;
                    // Increment usage count so the coupon cannot be reused beyond its limit
                    _couponService.ApplyCoupon(coupon.Id);
                }
            }

            var fullAddress = string.IsNullOrEmpty(districtName)
                ? $"{address}, {cityName}"
                : $"{address}, {districtName}, {cityName}";

            // Validate shippingFee server-side to prevent client manipulation
            if (shippingFee < 0) shippingFee = 0;

            var subTotal = itemsToOrder.Sum(i =>
                (i.Variant != null ? i.Variant.Price : i.Product?.Price ?? 0) * i.Quantity);
            var totalAmount = subTotal + shippingFee - discountAmount;

            var order = _orderService.CreateOrder(userId, itemsToOrder, fullAddress, paymentMethod, totalAmount, 0);

            // Xóa sản phẩm khỏi giỏ hàng NGAY LẬP TỨC nếu là thanh toán khi nhận hàng (COD).
            // Đối với VNPay/MoMo, giữ lại giỏ hàng và chỉ xóa khi thanh toán THÀNH CÔNG trong callback.
            if (paymentMethod == "COD")
            {
                _cartService.RemoveFromCart(itemsToOrder.Select(i => i.Id).ToList());
            }

            // Always redirect to ProcessPayment which will handle COD or redirect to MoMo/VNPay directly
            return RedirectToAction("ProcessPayment", "Payment", new { orderId = order.Id, paymentMethod = paymentMethod });
        }

        // GET: Order/History
        public IActionResult History(string? status)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized();
            }

            var orders = _orderService.GetUserOrders(userId);

            // Normalize the active tab key
            var activeTab = status?.ToLower() switch
            {
                "shipping" => "shipping",
                "delivered" => "delivered",
                "cancelled" => "cancelled",
                _ => "pending"
            };

            var filtered = activeTab switch
            {
                "shipping"  => orders.Where(o => o.Status == "Shipping"),
                "delivered" => orders.Where(o => o.Status == "Delivered"),
                "cancelled" => orders.Where(o => o.Status == "Cancelled" || o.Status == "Failed"),
                _           => orders.Where(o => o.Status == "Pending" || o.Status == "Processing")
            };

            // Counts for badge display
            ViewBag.CountPending   = orders.Count(o => o.Status == "Pending" || o.Status == "Processing");
            ViewBag.CountShipping  = orders.Count(o => o.Status == "Shipping");
            ViewBag.CountDelivered = orders.Count(o => o.Status == "Delivered");
            ViewBag.CountCancelled = orders.Count(o => o.Status == "Cancelled" || o.Status == "Failed");
            ViewBag.ActiveTab      = activeTab;

            return View(filtered);
        }

        // GET: Order/Detail/5
        public IActionResult Detail(int id)
        {
            var order = _orderService.GetOrderById(id);
            if (order == null)
            {
                return NotFound();
            }

            // Check if user owns this order
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (order.UserId != userId && !User.IsInRole("Admin"))
            {
                return Unauthorized();
            }

            return View(order);
        }

        // POST: Order/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            var order = _orderService.GetOrderById(id);
            if (order == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (order.UserId != userId && !User.IsInRole("Admin"))
            {
                return Unauthorized();
            }

            if (order.Status == "Pending" || order.Status == "Processing")
            {
                _orderService.CancelOrder(id);
                TempData["Success"] = "Đơn hàng đã được huỷ";
            }
            else
            {
                TempData["Error"] = "Không thể huỷ đơn hàng ở trạng thái hiện tại";
            }

            return RedirectToAction("Detail", new { id });
        }
    }
}
