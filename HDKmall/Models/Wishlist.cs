using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKmall.Models
{
    public class Wishlist
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public int? VersionId { get; set; }

        [ForeignKey("VersionId")]
        public virtual ProductVersion? ProductVersion { get; set; }

        public DateTime AddedDate { get; set; } = HDKmall.Helpers.TimeHelper.GetVietnamTime();
    }
}
