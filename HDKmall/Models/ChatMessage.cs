using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKmall.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; } // The customer involved in the conversation
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public int SenderId { get; set; } // Can be UserId or AdminId
        
        [Required]
        public string Message { get; set; }

        public DateTime Timestamp { get; set; } = HDKmall.Helpers.TimeHelper.GetVietnamTime();

        public bool IsFromAdmin { get; set; } = false;

        public bool IsRead { get; set; } = false;
    }
}
