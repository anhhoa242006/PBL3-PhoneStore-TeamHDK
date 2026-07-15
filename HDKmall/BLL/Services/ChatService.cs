using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HDKmall.BLL.Interfaces;
using HDKmall.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKmall.BLL.Services
{
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;

        public ChatService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> SendMessageAsync(ChatMessage message)
        {
            try
            {
                message.Timestamp = HDKmall.Helpers.TimeHelper.GetVietnamTime();
                _context.ChatMessages.Add(message);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<ChatMessage>> GetChatHistoryAsync(int userId)
        {
            return await _context.ChatMessages
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetActiveChatUsersAsync()
        {
            // Get unique users who have chat history
            var userIds = await _context.ChatMessages
                .Select(m => m.UserId)
                .Distinct()
                .ToListAsync();

            return await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId, bool forAdmin)
        {
            return await _context.ChatMessages
                .Where(m => m.UserId == userId && m.IsRead == false && m.IsFromAdmin == !forAdmin)
                .CountAsync();
        }

        public async Task MarkAsReadAsync(int userId, bool forAdmin)
        {
            var unread = await _context.ChatMessages
                .Where(m => m.UserId == userId && m.IsRead == false && m.IsFromAdmin == !forAdmin)
                .ToListAsync();

            foreach (var msg in unread)
            {
                msg.IsRead = true;
            }

            if (unread.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> DeleteChatHistoryAsync(int userId)
        {
            try
            {
                var messages = await _context.ChatMessages
                    .Where(m => m.UserId == userId)
                    .ToListAsync();

                if (messages.Any())
                {
                    _context.ChatMessages.RemoveRange(messages);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
