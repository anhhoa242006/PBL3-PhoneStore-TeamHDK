using HDKmall.ViewModels;
using HDKmall.Models;

namespace HDKmall.BLL.Interfaces
{
    public interface ICartService
    {
        ShoppingCart GetOrCreateCart(int? userId, string sessionId);
        int AddToCart(int cartId, int productId, int variantId, int quantity);
        void UpdateCartItem(int cartItemId, int quantity);
        void RemoveFromCart(int cartItemId);
        void RemoveFromCart(List<int> cartItemIds);
        void ClearCart(int cartId);
        ShoppingCart GetCartById(int cartId);
        int GetCartItemCount(int? userId, string sessionId);
    }
}
