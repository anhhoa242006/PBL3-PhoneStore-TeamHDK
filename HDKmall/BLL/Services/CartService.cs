using HDKmall.BLL.Interfaces;
using HDKmall.DAL.Interfaces;
using HDKmall.Models;
using System.Linq;
using System.Collections.Generic;

namespace HDKmall.BLL.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;

        public CartService(ICartRepository cartRepository, IProductRepository productRepository)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
        }

        public ShoppingCart GetOrCreateCart(int? userId, string sessionId)
        {
            ShoppingCart cart = null;

            if (userId.HasValue)
            {
                cart = _cartRepository.GetCartByUserId(userId.Value);
                if (cart == null)
                {
                    cart = new ShoppingCart { UserId = userId.Value };
                    _cartRepository.Add(cart);
                    _cartRepository.SaveChanges();
                }
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                cart = _cartRepository.GetCartBySessionId(sessionId);
                if (cart == null)
                {
                    cart = new ShoppingCart { SessionId = sessionId };
                    _cartRepository.Add(cart);
                    _cartRepository.SaveChanges();
                }
            }

            return cart;
        }

        public int AddToCart(int cartId, int productId, int variantId, int quantity)
        {
            var product = _productRepository.GetById(productId);
            // Không cho phép thêm sản phẩm đã ngừng bán vào giỏ hàng
            if (product == null || !product.IsActive) return 0;

            var cart = _cartRepository.GetCartById(cartId);
            if (cart == null) return 0;

            int? nullableVariantId = variantId > 0 ? variantId : (int?)null;

            var existingItem = cart.Items.FirstOrDefault(i =>
                i.ProductId == productId && i.VariantId == nullableVariantId);

            int resultId = 0;
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                _cartRepository.UpdateCartItem(existingItem);
                resultId = existingItem.Id;
            }
            else
            {
                var newItem = new CartItem
                {
                    ProductId = productId,
                    VariantId = nullableVariantId,
                    Quantity = quantity,
                    ShoppingCartId = cartId
                };
                cart.Items.Add(newItem);
                _cartRepository.Update(cart);
                // After SaveChanges, newItem.Id will be populated
                _cartRepository.SaveChanges();
                resultId = newItem.Id;
            }

            _cartRepository.SaveChanges();
            return resultId;
        }

        public void UpdateCartItem(int cartItemId, int quantity)
        {
            var item = _cartRepository.GetCartItemById(cartItemId);
            if (item == null) return;

            item.Quantity = quantity;
            _cartRepository.UpdateCartItem(item);
            _cartRepository.SaveChanges();
        }

        public void RemoveFromCart(int cartItemId)
        {
            var item = _cartRepository.GetCartItemById(cartItemId);
            if (item == null) return;

            _cartRepository.RemoveCartItem(item);
            _cartRepository.SaveChanges();
        }

        public void RemoveFromCart(List<int> cartItemIds)
        {
            if (cartItemIds == null || !cartItemIds.Any()) return;
            
            // Lấy tất cả items cần xóa trong 1 query duy nhất thay vì loop
            foreach (var id in cartItemIds)
            {
                var item = _cartRepository.GetCartItemById(id);
                if (item != null)
                {
                    _cartRepository.RemoveCartItem(item);
                }
            }
            _cartRepository.SaveChanges();
        }

        public void ClearCart(int cartId)
        {
            var cart = _cartRepository.GetCartById(cartId);
            if (cart != null)
            {
                foreach (var item in cart.Items.ToList())
                {
                    _cartRepository.RemoveCartItem(item);
                }
                _cartRepository.SaveChanges();
            }
        }

        public ShoppingCart GetCartById(int cartId)
        {
            return _cartRepository.GetCartById(cartId);
        }

        public int GetCartItemCount(int? userId, string sessionId)
        {
            ShoppingCart cart = null;
            if (userId.HasValue)
            {
                cart = _cartRepository.GetCartByUserId(userId.Value);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                cart = _cartRepository.GetCartBySessionId(sessionId);
            }
            return cart?.Items?.Sum(i => i.Quantity) ?? 0;
        }
    }
}
