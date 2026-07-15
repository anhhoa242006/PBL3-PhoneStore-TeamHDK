using System;
using System.Linq;
using System.Threading.Tasks;
using HDKmall.BLL.Interfaces;
using HDKmall.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HDKmall.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly IAIChatService _aiChatService;
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public ChatbotController(IAIChatService aiChatService, ApplicationDbContext context, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _aiChatService = aiChatService;
            _context = context;
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return BadRequest();
            }

            try
            {
                // 1. Lấy dữ liệu sản phẩm để làm ngữ cảnh (Context)
                // Lấy 10 sản phẩm mới nhất hoặc nổi bật
                var products = await _context.Products
                    .Include(p => p.Versions)
                    .ThenInclude(v => v.Specifications)
                    .OrderByDescending(p => p.Id)
                    .Take(10)
                    .ToListAsync();

                // Lấy các chương trình khuyến mãi đang diễn ra
                var now = HDKmall.Helpers.TimeHelper.GetVietnamTime();
                var promotions = await _context.Promotions
                    .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
                    .ToListAsync();

                // 2. Đọc thêm chính sách cửa hàng từ file tĩnh
                string storePolicy = "";
                try
                {
                    string policyPath = System.IO.Path.Combine(_env.WebRootPath, "StorePolicy.txt");
                    if (System.IO.File.Exists(policyPath))
                    {
                        storePolicy = await System.IO.File.ReadAllTextAsync(policyPath);
                    }
                }
                catch { }

                // 3. Xây dựng chuỗi Context cho AI
                var contextBuilder = new System.Text.StringBuilder();
                contextBuilder.AppendLine("BẠN LÀ CHUYÊN VIÊN TƯ VẤN BÁN HÀNG XUẤT SẮC CỦA HDKMALL.");
                contextBuilder.AppendLine("PHONG CÁCH: Xưng 'Dạ' và 'Em', gọi khách là 'Anh/Chị'. Thân thiện, ngắn gọn.");
                contextBuilder.AppendLine("QUY TẮC: Chỉ tư vấn dựa trên danh sách dưới đây. Nếu không có, báo hết hàng và gợi ý máy tương đương.");
                
                if (!string.IsNullOrEmpty(storePolicy))
                {
                    contextBuilder.AppendLine("\n[CHÍNH SÁCH VÀ THÔNG TIN CỬA HÀNG]:");
                    contextBuilder.AppendLine(storePolicy);
                }

                contextBuilder.AppendLine("\n[DANH SÁCH SẢN PHẨM TẠI SHOP]:");

                foreach (var p in products)
                {
                    contextBuilder.AppendLine($"- {p.Name}: Giá từ {p.Price:N0}đ.");
                    if (p.Versions.Any())
                    {
                        foreach (var v in p.Versions)
                        {
                            contextBuilder.AppendLine($"  + Phiên bản {v.Name}: {v.BasePrice:N0}đ. {v.Description}");
                            if (v.Specifications.Any())
                            {
                                var specs = string.Join(", ", v.Specifications.Take(3).Select(s => $"{s.SpecName}: {s.SpecValue}"));
                                contextBuilder.AppendLine($"    * Cấu hình: {specs}");
                            }
                        }
                    }
                }

                if (promotions.Any())
                {
                    contextBuilder.AppendLine("\n[CHƯƠNG TRÌNH KHUYẾN MÃI]:");
                    foreach (var promo in promotions)
                    {
                        contextBuilder.AppendLine($"- {promo.Name}: {promo.Description}");
                    }
                }

                // 3. Gọi Service AI
                var aiResponse = await _aiChatService.GetResponseAsync(request.Message, contextBuilder.ToString());

                return Json(new { response = aiResponse });
            }
            catch (Exception ex)
            {
                return Json(new { response = "Dạ, em đang gặp chút vấn đề kỹ thuật. Anh/Chị vui lòng hỏi lại sau nhé!" });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
}
