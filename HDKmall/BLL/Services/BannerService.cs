using HDKmall.BLL.Interfaces;
using HDKmall.DAL.Interfaces;
using HDKmall.Models;

namespace HDKmall.BLL.Services
{
    public class BannerService : IBannerService
    {
        private readonly IBannerRepository _bannerRepository;

        public BannerService(IBannerRepository bannerRepository)
        {
            _bannerRepository = bannerRepository;
        }

        public IEnumerable<Banner> GetAllBanners()
        {
            return _bannerRepository.GetAll();
        }

        public Banner GetBannerById(int id)
        {
            return _bannerRepository.GetById(id);
        }

        public void CreateBanner(Banner banner)
        {
            _bannerRepository.Add(banner);
            _bannerRepository.SaveChanges();
        }

        public void UpdateBanner(int id, Banner banner)
        {
            var existing = _bannerRepository.GetById(id);
            if (existing != null)
            {
                existing.ImageUrl = banner.ImageUrl;
                existing.Title = banner.Title;
                existing.Description = banner.Description;
                existing.BadgeText = banner.BadgeText;
                existing.ButtonText = banner.ButtonText;
                existing.LinkUrl = banner.LinkUrl;
                existing.IsActive = banner.IsActive;
                _bannerRepository.Update(existing);
                _bannerRepository.SaveChanges();
            }
        }

        public void DeleteBanner(int id)
        {
            _bannerRepository.Delete(id);
            _bannerRepository.SaveChanges();
        }
    }
}
