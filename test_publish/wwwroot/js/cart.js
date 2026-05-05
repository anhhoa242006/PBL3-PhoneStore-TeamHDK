// =====================
// CART MANAGEMENT
// =====================

const CART_KEY = 'phonestore_cart';

const Cart = {
  getItems() {
    try {
      return JSON.parse(localStorage.getItem(CART_KEY)) || [];
    } catch {
      return [];
    }
  },

  saveItems(items) {
    localStorage.setItem(CART_KEY, JSON.stringify(items));
    this.updateCartBadge();
    window.dispatchEvent(new CustomEvent('cartUpdated', { detail: { items } }));
  },

  addItem(product, variant, quantity = 1) {
    const items = this.getItems();
    const variantKey = `${product.id}-${variant.color}-${variant.storage}`;
    const existingIndex = items.findIndex(i => i.variantKey === variantKey);

    if (existingIndex >= 0) {
      items[existingIndex].quantity += quantity;
    } else {
      items.push({
        variantKey,
        productId: product.id,
        name: product.name,
        image: product.image,
        brand: product.brand,
        price: variant.price || product.price,
        originalPrice: product.originalPrice,
        color: variant.color,
        storage: variant.storage,
        quantity,
      });
    }

    this.saveItems(items);
    this.showToast(`Đã thêm "${product.name}" vào giỏ hàng!`);
  },

  removeItem(variantKey) {
    const items = this.getItems().filter(i => i.variantKey !== variantKey);
    this.saveItems(items);
  },

  updateQuantity(variantKey, quantity) {
    if (quantity < 1) {
      this.removeItem(variantKey);
      return;
    }
    const items = this.getItems();
    const idx = items.findIndex(i => i.variantKey === variantKey);
    if (idx >= 0) {
      items[idx].quantity = quantity;
      this.saveItems(items);
    }
  },

  clearCart() {
    this.saveItems([]);
  },

  getTotalItems() {
    return this.getItems().reduce((sum, i) => sum + i.quantity, 0);
  },

  getTotalPrice() {
    return this.getItems().reduce((sum, i) => sum + i.price * i.quantity, 0);
  },

  getTotalDiscount() {
    return this.getItems().reduce((sum, i) => {
      const discount = (i.originalPrice - i.price) * i.quantity;
      return sum + (discount > 0 ? discount : 0);
    }, 0);
  },

  updateCartBadge() {
    const badge = document.getElementById('cart-badge');
    if (badge) {
      const count = this.getTotalItems();
      badge.textContent = count;
      badge.style.display = count > 0 ? 'flex' : 'none';
    }
  },

  showToast(message, type = 'success') {
    // Remove existing toast
    const existing = document.getElementById('toast-notification');
    if (existing) existing.remove();

    const toast = document.createElement('div');
    toast.id = 'toast-notification';
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `
      <span class="toast-icon">${type === 'success' ? '✓' : '✕'}</span>
      <span class="toast-message">${message}</span>
    `;
    document.body.appendChild(toast);

    // Trigger animation
    requestAnimationFrame(() => {
      toast.classList.add('toast-show');
    });

    setTimeout(() => {
      toast.classList.remove('toast-show');
      setTimeout(() => toast.remove(), 300);
    }, 3000);
  },
};

// Initialize cart badge on load
document.addEventListener('DOMContentLoaded', () => {
  Cart.updateCartBadge();
});
