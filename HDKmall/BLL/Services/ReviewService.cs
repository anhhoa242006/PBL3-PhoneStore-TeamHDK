using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HDKmall.BLL.Interfaces;
using HDKmall.DAL.Interfaces;
using HDKmall.Models;
using HDKmall.ViewModels;

namespace HDKmall.BLL.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IPhotoService _photoService;

        public ReviewService(
            IReviewRepository reviewRepository,
            IOrderRepository orderRepository,
            IPhotoService photoService)
        {
            _reviewRepository = reviewRepository;
            _orderRepository = orderRepository;
            _photoService = photoService;
        }

        public async Task<Review> AddReviewAsync(AddReviewVM vm, int userId)
        {
            // Check if user already reviewed this version -> update instead
            var existing = _reviewRepository.GetUserReviewForVersion(userId, vm.ProductVersionId);
            if (existing != null)
            {
                var updated = await EditReviewAsync(existing.Id, vm, userId);
                return updated ?? existing;
            }

            var review = new Review
            {
                ProductVersionId = vm.ProductVersionId,
                UserId = userId,
                Rating = Math.Min(5, Math.Max(1, vm.Rating)),
                Comment = vm.Comment,
                CreatedAt = HDKmall.Helpers.TimeHelper.GetVietnamTime(),
                Status = "Approved"
            };

            if (vm.Tags != null && vm.Tags.Any())
            {
                review.Tags = JsonSerializer.Serialize(vm.Tags);
            }

            if (vm.ImageFile != null)
            {
                var uploadResult = await _photoService.AddPhotoAsync(vm.ImageFile);
                if (uploadResult.Error == null)
                {
                    review.ImageUrl = uploadResult.SecureUrl.ToString();
                }
            }

            _reviewRepository.Add(review);
            _reviewRepository.SaveChanges();
            return review;
        }

        public async Task<Review?> EditReviewAsync(int reviewId, AddReviewVM vm, int userId)
        {
            var review = _reviewRepository.GetById(reviewId);
            if (review == null || review.UserId != userId) return null;

            // Enforce one-time edit
            if (review.IsEdited) return null;

            review.Rating = Math.Min(5, Math.Max(1, vm.Rating));
            review.Comment = vm.Comment;
            review.Tags = (vm.Tags != null && vm.Tags.Any()) ? JsonSerializer.Serialize(vm.Tags) : null;

            if (vm.ImageFile != null)
            {
                var uploadResult = await _photoService.AddPhotoAsync(vm.ImageFile);
                if (uploadResult.Error == null)
                {
                    review.ImageUrl = uploadResult.SecureUrl.ToString();
                }
            }

            review.IsEdited = true; // Mark as edited
            _reviewRepository.Update(review);
            _reviewRepository.SaveChanges();
            return review;
        }

        public IEnumerable<ReviewVM> GetProductReviews(int productVersionId)
        {
            var reviews = _reviewRepository.GetApprovedByVersionId(productVersionId);
            return MapToVM(reviews);
        }

        public IEnumerable<ReviewVM> GetProductAllReviews(int productId)
        {
            var reviews = _reviewRepository.GetApprovedByProductId(productId);
            return MapToVM(reviews);
        }

        private IEnumerable<ReviewVM> MapToVM(IEnumerable<Review> reviews)
        {
            return reviews.Select(r => new ReviewVM
            {
                Id = r.Id,
                ProductVersionId = r.ProductVersionId,
                UserId = r.UserId,
                UserName = r.User?.FullName ?? "Ẩn danh",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                Tags = r.Tags,
                ImageUrl = r.ImageUrl,
                Status = r.Status,
                IsEdited = r.IsEdited,
                AdminReply = r.AdminReply,
                AdminReplyAt = r.AdminReplyAt,
                ProductVersionName = r.ProductVersion?.Name
            });
        }

        public double GetAverageRating(int productVersionId)
        {
            var reviews = _reviewRepository.GetApprovedByVersionId(productVersionId).ToList();
            if (!reviews.Any()) return 0;
            return reviews.Average(r => r.Rating);
        }

        public double GetAverageRatingByProduct(int productId)
        {
            var reviews = _reviewRepository.GetApprovedByProductId(productId).ToList();
            if (!reviews.Any()) return 0;
            return reviews.Average(r => r.Rating);
        }

        public void DeleteReview(int id)
        {
            _reviewRepository.Delete(id);
            _reviewRepository.SaveChanges();
        }

        public bool UserCanReview(int userId, int productVersionId)
        {
            return HasUserBoughtVersion(userId, productVersionId);
        }

        public bool HasUserBoughtVersion(int userId, int productVersionId)
        {
            var userOrders = _orderRepository.GetOrdersByUserId(userId);
            if (userOrders == null) return false;

            return userOrders.Any(o =>
                o.Status == "Delivered" &&
                o.OrderDetails != null &&
                o.OrderDetails.Any(od => od.ProductVariant != null && od.ProductVariant.ProductVersionId == productVersionId));
        }

        public Review? GetUserReviewForVersion(int userId, int versionId)
        {
            return _reviewRepository.GetUserReviewForVersion(userId, versionId);
        }

        public Review? GetUserReviewForProduct(int userId, int productId)
        {
            var reviews = _reviewRepository.GetByUserId(userId);
            return reviews.FirstOrDefault(r =>
                r.ProductVersion != null &&
                r.ProductVersion.ProductId == productId);
        }

        public IEnumerable<Review> GetAllReviews()
        {
            return _reviewRepository.GetAll();
        }

        public void ApproveReview(int id)
        {
            _reviewRepository.UpdateStatus(id, "Approved");
        }

        public void HideReview(int id)
        {
            _reviewRepository.UpdateStatus(id, "Hidden");
        }

        public void ReplyToReview(int id, string? replyText)
        {
            var review = _reviewRepository.GetById(id);
            if (review != null)
            {
                if (string.IsNullOrWhiteSpace(replyText))
                {
                    review.AdminReply = null;
                    review.AdminReplyAt = null;
                }
                else
                {
                    review.AdminReply = replyText;
                    review.AdminReplyAt = HDKmall.Helpers.TimeHelper.GetVietnamTime();
                }
                _reviewRepository.Update(review);
                _reviewRepository.SaveChanges();
            }
        }
    }
}