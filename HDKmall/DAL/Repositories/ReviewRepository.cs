using System.Collections.Generic;
using System.Linq;
using HDKmall.DAL.Interfaces;
using HDKmall.Models;
using Microsoft.EntityFrameworkCore;

namespace HDKmall.DAL.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly ApplicationDbContext _context;

        public ReviewRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Review GetById(int id)
        {
            return _context.Reviews
                .Include(r => r.User)
                .Include(r => r.ProductVersion)
                .ThenInclude(v => v.Product)
                .FirstOrDefault(r => r.Id == id);
        }

        public IEnumerable<Review> GetByVersionId(int versionId)
        {
            return _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductVersionId == versionId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public IEnumerable<Review> GetByUserId(int userId)
        {
            return _context.Reviews
                .Include(r => r.ProductVersion)
                .ThenInclude(v => v.Product)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public IEnumerable<Review> GetAll()
        {
            return _context.Reviews
                .Include(r => r.User)
                .Include(r => r.ProductVersion)
                .ThenInclude(v => v.Product)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public IEnumerable<Review> GetByProductId(int productId)
        {
            return _context.Reviews
                .Include(r => r.User)
                .Include(r => r.ProductVersion)
                .Where(r => r.ProductVersion.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public Review? GetUserReviewForVersion(int userId, int versionId)
        {
            return _context.Reviews
                .Include(r => r.User)
                .FirstOrDefault(r => r.UserId == userId && r.ProductVersionId == versionId);
        }

        public void Add(Review review)
        {
            _context.Reviews.Add(review);
        }

        public void Update(Review review)
        {
            _context.Reviews.Update(review);
        }

        public void Delete(int id)
        {
            var review = GetById(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
            }
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        public IEnumerable<Review> GetApprovedByVersionId(int versionId)
        {
            return _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductVersionId == versionId && r.Status != "Hidden")
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public IEnumerable<Review> GetApprovedByProductId(int productId)
        {
            return _context.Reviews
                .Include(r => r.User)
                .Include(r => r.ProductVersion)
                .Where(r => r.ProductVersion.ProductId == productId && r.Status != "Hidden")
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public void UpdateStatus(int id, string status)
        {
            var review = _context.Reviews.FirstOrDefault(r => r.Id == id);
            if (review != null)
            {
                review.Status = status;
                _context.Reviews.Update(review);
                _context.SaveChanges();
            }
        }
    }
}
