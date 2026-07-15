using System;
using System.Security.Claims;
using System.Threading.Tasks;
using HDKmall.BLL.Interfaces;
using HDKmall.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HDKmall.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.ActiveTab = "contacts";
            var users = await _chatService.GetActiveChatUsersAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory(int userId)
        {
            var history = await _chatService.GetChatHistoryAsync(userId);
            await _chatService.MarkAsReadAsync(userId, true);
            return Json(history);
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] AdminChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message)) return BadRequest();

            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(adminIdStr, out int adminId)) return BadRequest();

            var chatMsg = new ChatMessage
            {
                UserId = request.UserId,
                SenderId = adminId,
                Message = request.Message,
                IsFromAdmin = true,
                Timestamp = HDKmall.Helpers.TimeHelper.GetVietnamTime()
            };

            var result = await _chatService.SendMessageAsync(chatMsg);
            return Json(new { success = result });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteHistory(int userId)
        {
            var result = await _chatService.DeleteChatHistoryAsync(userId);
            return Json(new { success = result });
        }
    }

    public class AdminChatRequest
    {
        public int UserId { get; set; }
        public string Message { get; set; }
    }
}
