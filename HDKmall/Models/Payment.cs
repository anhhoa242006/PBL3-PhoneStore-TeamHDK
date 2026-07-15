using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKmall.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public string? TransactionId { get; set; }

        public DateTime CreatedAt { get; set; } = HDKmall.Helpers.TimeHelper.GetVietnamTime();

        public DateTime? UpdatedAt { get; set; }
    }
}
