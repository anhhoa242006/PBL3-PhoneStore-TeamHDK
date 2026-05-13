namespace HDKmall.ViewModels
{
    public class ProductImageVM
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public string? PublicId { get; set; }
        public bool IsMain { get; set; }
        public bool IsDeleted { get; set; }
        public int DisplayOrder { get; set; }
    }
}
