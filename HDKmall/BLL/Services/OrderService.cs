using HDKmall.BLL.Interfaces;
using HDKmall.DAL.Interfaces;
using HDKmall.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HDKmall.BLL.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository, ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _logger = logger;
        }

        public Order CreateOrder(int userId, List<CartItem> items, string address, string paymentMethod, decimal totalAmount, decimal discountAmount = 0)
        {
            // Initial status: COD -> Processing, others -> AwaitingPayment
            string initialStatus = (paymentMethod == "COD") ? "Processing" : "AwaitingPayment";

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Address = address,
                PaymentMethod = paymentMethod,
                TotalAmount = totalAmount - discountAmount,
                Status = initialStatus,
                OrderDetails = items.Select(i => new OrderDetail
                {
                    ProductId = i.ProductId,
                    ProductVariantId = i.VariantId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Variant != null ? i.Variant.Price : (i.Product?.Price ?? 0)
                }).ToList()
            };

            _orderRepository.Add(order);
            _orderRepository.SaveChanges();

            // Deduct stock
            AdjustStock(order, true);

            _logger.LogInformation("Đã tạo đơn hàng mới #{OrderId} cho người dùng {UserId}. Trạng thái: {Status}. Tổng tiền: {TotalAmount}", order.Id, userId, initialStatus, order.TotalAmount);
            return order;
        }

        public IEnumerable<Order> GetUserOrders(int userId)
        {
            return _orderRepository.GetOrdersByUserId(userId);
        }

        public IEnumerable<Order> GetAllOrders()
        {
            return _orderRepository.GetAll();
        }

        public Order GetOrderById(int id)
        {
            return _orderRepository.GetById(id);
        }

        public void UpdateOrderStatus(int id, string status)
        {
            var order = _orderRepository.GetById(id);
            if (order != null)
            {
                var oldStatus = order.Status;
                
                // If moving to Cancelled or Failed, return stock
                if ((status == "Cancelled" || status == "Failed") && (oldStatus != "Cancelled" && oldStatus != "Failed"))
                {
                    AdjustStock(order, false);
                }

                order.Status = status;
                _orderRepository.Update(order);
                _orderRepository.SaveChanges();
                _logger.LogInformation("Đơn hàng #{OrderId} đã chuyển trạng thái từ '{OldStatus}' sang '{NewStatus}'", id, oldStatus, status);
            }
        }

        public void CancelOrder(int id)
        {
            UpdateOrderStatus(id, "Cancelled");
        }

        public void DeleteOrder(int id)
        {
            var order = _orderRepository.GetById(id);
            if (order != null)
            {
                // If not already cancelled/failed, return stock before deleting
                if (order.Status != "Cancelled" && order.Status != "Failed")
                {
                    AdjustStock(order, false);
                }

                _orderRepository.Delete(id);
                _orderRepository.SaveChanges();
                _logger.LogInformation("Đã xóa hoàn toàn đơn hàng #{OrderId} do thanh toán thất bại/hủy", id);
            }
        }

        private void AdjustStock(Order order, bool isDeducting)
        {
            if (order.OrderDetails == null) return;

            foreach (var detail in order.OrderDetails)
            {
                if (detail.ProductVariantId.HasValue)
                {
                    // Handle Stock at Variant level
                    var product = _productRepository.GetById(detail.ProductId);
                    if (product != null && product.Versions != null)
                    {
                        var variant = product.Versions.SelectMany(v => v.Variants).FirstOrDefault(v => v.Id == detail.ProductVariantId.Value);
                        if (variant != null)
                        {
                            if (isDeducting)
                                variant.Stock -= detail.Quantity;
                            else
                                variant.Stock += detail.Quantity;

                            _productRepository.UpdateVariant(variant);
                        }
                    }
                }
            }
            _productRepository.SaveChanges();
        }
    }
}
