using HDKmall.BLL.Interfaces;
using HDKmall.Models;
using HDKmall.ViewModels;

namespace HDKmall.BLL.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;

        public PaymentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void CreatePayment(int orderId, string paymentMethod, decimal amount)
        {
            // Avoid duplicate entries (idempotent)
            if (_context.Payments.Any(p => p.OrderId == orderId))
                return;

            var payment = new Payment
            {
                OrderId = orderId,
                PaymentMethod = paymentMethod,
                Amount = amount,
                Status = "Pending",
                CreatedAt = HDKmall.Helpers.TimeHelper.GetVietnamTime()
            };

            _context.Payments.Add(payment);
            _context.SaveChanges();
        }

        public PaymentHistoryVM GetPaymentHistory(int orderId)
        {
            var payment = _context.Payments.FirstOrDefault(p => p.OrderId == orderId);
            if (payment == null) return null;

            return new PaymentHistoryVM
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                PaymentMethod = payment.PaymentMethod,
                Amount = payment.Amount,
                Status = payment.Status,
                TransactionId = payment.TransactionId,
                CreatedAt = payment.CreatedAt
            };
        }

        public void UpdatePaymentStatus(int orderId, string status, string transactionId)
        {
            var payment = _context.Payments.FirstOrDefault(p => p.OrderId == orderId);
            if (payment != null)
            {
                payment.Status = status;
                payment.TransactionId = transactionId;
                payment.UpdatedAt = HDKmall.Helpers.TimeHelper.GetVietnamTime();
                _context.SaveChanges();
            }
        }
    }
}
