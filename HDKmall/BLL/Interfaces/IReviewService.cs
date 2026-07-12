using System.Collections.Generic;
using System.Threading.Tasks;
using HDKmall.Models;
using HDKmall.ViewModels;

namespace HDKmall.BLL.Interfaces
{
    public interface IReviewService
    {
        IEnumerable<ReviewVM> GetProductReviews(int productVersionId);
        IEnumerable<ReviewVM> GetProductAllReviews(int productId);
        IEnumerable<Review> GetAllReviews();
        void DeleteReview(int id);
        double GetAverageRating(int productVersionId);
        double GetAverageRatingByProduct(int productId);
        bool UserCanReview(int userId, int productVersionId);
        bool HasUserBoughtVersion(int userId, int productVersionId);
        Review? GetUserReviewForVersion(int userId, int versionId);
        Review? GetUserReviewForProduct(int userId, int productId);
        Task<Review> AddReviewAsync(AddReviewVM vm, int userId);
        Task<Review?> EditReviewAsync(int reviewId, AddReviewVM vm, int userId);
        void ApproveReview(int id);
        void HideReview(int id);
        void ReplyToReview(int id, string? replyText);
    }
}
