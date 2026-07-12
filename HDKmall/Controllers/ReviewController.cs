using System.Security.Claims;
using System.Threading.Tasks;
using HDKmall.BLL.Interfaces;
using HDKmall.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.Json;
using HDKmall.Models;

namespace HDKmall.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly HDKmall.DAL.Interfaces.IProductRepository _productRepo;

        public ReviewController(IReviewService reviewService, HDKmall.DAL.Interfaces.IProductRepository productRepo)
        {
            _reviewService = reviewService;
            _productRepo = productRepo;
        }

        [HttpPost]
        public async Task<IActionResult> Add(AddReviewVM vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin đánh giá.";
                return RedirectToRequest();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                // Check if user bought the product (via version)
                if (!_reviewService.UserCanReview(userId, vm.ProductVersionId))
                {
                    TempData["Error"] = "Bạn cần mua sản phẩm này trước khi đánh giá.";
                    return RedirectToRequest();
                }

                // AddReviewAsync handles edit if already reviewed
                await _reviewService.AddReviewAsync(vm, userId);
                TempData["Success"] = "Cảm ơn bạn! Đánh giá của bạn đã được lưu.";
            }
            else
            {
                TempData["Error"] = "Vui lòng đăng nhập để đánh giá.";
            }

            return RedirectToRequest();
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int reviewId, AddReviewVM vm)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                TempData["Error"] = "Vui lòng đăng nhập.";
                return RedirectToRequest();
            }

            var result = await _reviewService.EditReviewAsync(reviewId, vm, userId);
            if (result == null)
            {
                TempData["Error"] = "Không thể sửa đánh giá này.";
            }
            else
            {
                TempData["Success"] = "Đánh giá của bạn đã được cập nhật.";
            }

            return RedirectToRequest();
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            _reviewService.DeleteReview(id);
            TempData["Success"] = "Đánh giá đã được xoá";
            return RedirectToRequest();
        }

        // GET: Review/GetByProduct?productId=5&sort=newest&star=0
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetByProduct(int productId, string sort = "newest", int star = 0)
        {
            var reviews = _reviewService.GetProductAllReviews(productId).ToList();

            if (star > 0)
                reviews = reviews.Where(r => r.Rating == star).ToList();

            if (sort == "rating-high")
                reviews = reviews.OrderByDescending(r => r.Rating).ToList();
            else if (sort == "rating-low")
                reviews = reviews.OrderBy(r => r.Rating).ToList();
            else // newest
                reviews = reviews.OrderByDescending(r => r.CreatedAt).ToList();

            var result = reviews.Select(r => new
            {
                r.Id,
                r.UserName,
                r.Rating,
                r.Comment,
                r.ImageUrl,
                Tags = r.Tags != null ? JsonSerializer.Deserialize<List<string>>(r.Tags) : new List<string>(),
                CreatedAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                r.AdminReply,
                AdminReplyAt = r.AdminReplyAt.HasValue ? r.AdminReplyAt.Value.ToString("dd/MM/yyyy HH:mm") : null
            });

            return Json(new
            {
                reviews = result,
                total = reviews.Count,
                average = reviews.Any() ? reviews.Average(r => r.Rating) : 0
            });
        }

        private IActionResult RedirectToRequest()
        {
            var referer = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(referer)) return RedirectToAction("Index", "Home");
            return Redirect(referer + "#review-section");
        }
    }
}
