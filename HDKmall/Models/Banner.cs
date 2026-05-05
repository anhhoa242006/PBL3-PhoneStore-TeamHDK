using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HDKmall.Models
{
    public class Banner
    {
        [Key]
        public int Id { get; set; }
        public string ImageUrl { get; set; } = "";
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? BadgeText { get; set; }
        public string? ButtonText { get; set; }
        public string? LinkUrl { get; set; }
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
