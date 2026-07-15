using HDKmall.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HDKmall.BLL.Interfaces;

namespace HDKmall.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PromotionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPhotoService _photoService;

        public PromotionController(ApplicationDbContext context, IPhotoService photoService)
        {
            _context = context;
            _photoService = photoService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveTab = "promotions";
            var promotions = await _context.Promotions.OrderByDescending(p => p.Id).ToListAsync();
            return View(promotions);
        }

        public IActionResult Create()
        {
            ViewBag.ActiveTab = "promotions";
            return View(new Promotion { StartDate = HDKmall.Helpers.TimeHelper.GetVietnamTime(), EndDate = HDKmall.Helpers.TimeHelper.GetVietnamTime().AddDays(7), IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Promotion promotion)
        {
            if (ModelState.IsValid)
            {
                if (promotion.ImageFile != null && promotion.ImageFile.Length > 0)
                {
                    var result = await _photoService.AddPhotoAsync(promotion.ImageFile);
                    if (result.Error == null)
                    {
                        promotion.BannerUrl = result.SecureUrl.AbsoluteUri;
                    }
                }

                _context.Add(promotion);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo chiến dịch khuyến mãi thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.ActiveTab = "promotions";
            return View(promotion);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();
            
            ViewBag.ActiveTab = "promotions";
            return View(promotion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Promotion promotion)
        {
            if (id != promotion.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (promotion.ImageFile != null && promotion.ImageFile.Length > 0)
                    {
                        var result = await _photoService.AddPhotoAsync(promotion.ImageFile);
                        if (result.Error == null)
                        {
                            promotion.BannerUrl = result.SecureUrl.AbsoluteUri;
                        }
                    }

                    _context.Update(promotion);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật chiến dịch thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PromotionExists(promotion.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.ActiveTab = "promotions";
            return View(promotion);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion != null)
            {
                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        private bool PromotionExists(int id)
        {
            return _context.Promotions.Any(e => e.Id == id);
        }
    }
}
