using System.ComponentModel.DataAnnotations;

namespace HDKmall.Models
{
    public class Promotion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public string? BannerUrl { get; set; }

        public DateTime StartDate { get; set; } = HDKmall.Helpers.TimeHelper.GetVietnamTime();

        public DateTime EndDate { get; set; } = HDKmall.Helpers.TimeHelper.GetVietnamTime().AddDays(30);

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public Microsoft.AspNetCore.Http.IFormFile? ImageFile { get; set; }
    }
}
