using System;
using System.Security.Claims;
using System.Threading.Tasks;
using HDKmall.BLL.Interfaces;
using HDKmall.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HDKmall.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return BadRequest();

            var history = await _chatService.GetChatHistoryAsync(userId);
            await _chatService.MarkAsReadAsync(userId, false);
            return Json(history);
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return BadRequest();

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return BadRequest();

            var chatMsg = new ChatMessage
            {
                UserId = userId,
                SenderId = userId,
                Message = message,
                IsFromAdmin = false,
                Timestamp = HDKmall.Helpers.TimeHelper.GetVietnamTime()
            };

            var result = await _chatService.SendMessageAsync(chatMsg);
            return Json(new { success = result });
        }
    }
}
