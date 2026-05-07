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

        public void AddToCart(int cartId, int productId, int variantId, int quantity)
        {
            var product = _productRepository.GetById(productId);
            // Không cho phép thêm sản phẩm đã ngừng bán vào giỏ hàng
            if (product == null || !product.IsActive) return;

            var cart = _cartRepository.GetCartById(cartId);
            if (cart == null) return;

            int? nullableVariantId = variantId > 0 ? variantId : (int?)null;

            var existingItem = cart.Items.FirstOrDefault(i =>
                i.ProductId == productId && i.VariantId == nullableVariantId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                _cartRepository.UpdateCartItem(existingItem);
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = productId,
                    VariantId = nullableVariantId,
                    Quantity = quantity,
                    ShoppingCartId = cartId
                });
                _cartRepository.Update(cart);
            }

            _cartRepository.SaveChanges();
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
