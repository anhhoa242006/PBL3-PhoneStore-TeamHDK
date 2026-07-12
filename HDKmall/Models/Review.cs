using System;
using System.ComponentModel.DataAnnotations;

namespace HDKmall.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }
        public int ProductVersionId { get; set; }
        public ProductVersion ProductVersion { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? Tags { get; set; }        // JSON array string
         public string? ImageUrl { get; set; }    // Cloudinary URL
         public string? AdminReply { get; set; }
         public DateTime? AdminReplyAt { get; set; }
         public string Status { get; set; } = "Approved"; // Pending/Approved/Hidden
         public bool IsEdited { get; set; } = false;
    }
}
