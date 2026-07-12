using HDKmall.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq; 

namespace HDKmall.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        public IActionResult Index(string? q, string? status) // Thêm tham số status
{
    ViewBag.ActiveTab = "reviews";
    var reviews = _reviewService.GetAllReviews();

    // 1. Lọc theo trạng thái
    if (!string.IsNullOrEmpty(status) && status != "All")
    {
        if (status == "Showing")
        {
            reviews = reviews.Where(r => r.Status != "Hidden");
        }
        else
        {
            reviews = reviews.Where(r => r.Status == status);
        }
    }

    // 2. Lọc theo từ khóa tìm kiếm
    if (!string.IsNullOrWhiteSpace(q))
    {
        var key = q.Trim().ToLower();
        reviews = reviews.Where(r =>
            (r.ProductVersion?.Product?.Name ?? "").ToLower().Contains(key) ||
            (r.User?.FullName ?? "").ToLower().Contains(key) ||
            (r.Comment ?? "").ToLower().Contains(key)
        );
    }

    ViewBag.Search = q;
    ViewBag.Status = status ?? "All"; // Gửi trạng thái hiện tại xuống View
    return View(reviews);
}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            _reviewService.DeleteReview(id);
            TempData["Success"] = "Đã xóa đánh giá.";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken] // Thêm bảo mật giống hàm Delete của bạn
        public IActionResult Approve(int id)
        {
            _reviewService.ApproveReview(id);
            TempData["Success"] = "Đã duyệt đánh giá. Đánh giá này sẽ được hiển thị với khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Thêm bảo mật giống hàm Delete của bạn
        public IActionResult Hide(int id)
        {
            _reviewService.HideReview(id);
            TempData["Success"] = "Đã ẩn đánh giá. Đánh giá này không còn hiển thị với khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reply(int id, string? replyText)
        {
            _reviewService.ReplyToReview(id, replyText);
            TempData["Success"] = "Đã cập nhật phản hồi thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}