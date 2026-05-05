// =====================
// ORDERS.JS - Order Tracking
// =====================

const ORDERS_KEY = 'phonestore_orders';

const Orders = {
  getAll() {
    try {
      return JSON.parse(localStorage.getItem(ORDERS_KEY)) || [];
    } catch {
      return [];
    }
  },

  getUserOrders(userId) {
    return this.getAll().filter(o => o.userId === userId);
  },

  getById(orderId) {
    return this.getAll().find(o => o.id === orderId) || null;
  },

  // Create a new order from current cart (called from checkout)
  createOrder(userId, userInfo, cartItems, paymentMethod) {
    const orders = this.getAll();
    const order = {
      id: 'ĐH' + Date.now(),
      userId,
      userInfo,
      items: cartItems,
      paymentMethod,
      status: 'pending',
      statusHistory: [
        { status: 'pending', label: 'Đã đặt hàng', time: new Date().toISOString() },
      ],
      total: cartItems.reduce((s, i) => s + i.price * i.quantity, 0),
      createdAt: new Date().toISOString(),
    };
    orders.push(order);
    localStorage.setItem(ORDERS_KEY, JSON.stringify(orders));
    return order;
  },

  // Seed demo orders for a user (for demonstration)
  seedDemoOrders(userId) {
    const existing = this.getUserOrders(userId);
    if (existing.length > 0) return;

    const now = Date.now();
    const demo = [
      {
        id: 'ĐH' + (now - 86400000 * 3),
        userId,
        userInfo: { name: 'Người dùng', phone: '0901234567', address: '123 Nguyễn Văn Linh, Đà Nẵng' },
        items: [
          { name: 'iPhone 17 Pro Max', image: 'https://placehold.co/80x80/1a1a2e/ffffff?text=iPhone', price: 34990000, quantity: 1, color: 'Titan Tự Nhiên', storage: '256GB' },
        ],
        paymentMethod: 'COD',
        status: 'delivered',
        statusHistory: [
          { status: 'pending', label: 'Đã đặt hàng', time: new Date(now - 86400000 * 3).toISOString() },
          { status: 'confirmed', label: 'Đã xác nhận', time: new Date(now - 86400000 * 2.5).toISOString() },
          { status: 'shipping', label: 'Đang giao hàng', time: new Date(now - 86400000 * 1).toISOString() },
          { status: 'delivered', label: 'Đã giao hàng', time: new Date(now - 86400000 * 0.2).toISOString() },
        ],
        total: 34990000,
        createdAt: new Date(now - 86400000 * 3).toISOString(),
      },
      {
        id: 'ĐH' + (now - 86400000 * 1),
        userId,
        userInfo: { name: 'Người dùng', phone: '0901234567', address: '123 Nguyễn Văn Linh, Đà Nẵng' },
        items: [
          { name: 'AirPods 4 Pro', image: 'https://placehold.co/80x80/a80016/ffffff?text=AirPods', price: 4990000, quantity: 2, color: 'Trắng', storage: '' },
        ],
        paymentMethod: 'Thẻ ngân hàng',
        status: 'shipping',
        statusHistory: [
          { status: 'pending', label: 'Đã đặt hàng', time: new Date(now - 86400000 * 1).toISOString() },
          { status: 'confirmed', label: 'Đã xác nhận', time: new Date(now - 86400000 * 0.8).toISOString() },
          { status: 'shipping', label: 'Đang giao hàng', time: new Date(now - 86400000 * 0.3).toISOString() },
        ],
        total: 9980000,
        createdAt: new Date(now - 86400000 * 1).toISOString(),
      },
    ];

    const all = this.getAll();
    all.push(...demo);
    localStorage.setItem(ORDERS_KEY, JSON.stringify(all));
  },
};

// ---- Status helpers ----
const ORDER_STATUSES = [
  { key: 'pending',   label: 'Đã đặt hàng',    icon: '📋' },
  { key: 'confirmed', label: 'Đã xác nhận',     icon: '✅' },
  { key: 'shipping',  label: 'Đang giao hàng',  icon: '🚚' },
  { key: 'delivered', label: 'Đã giao hàng',    icon: '🎉' },
  { key: 'cancelled', label: 'Đã hủy',          icon: '❌' },
];

function getStatusLabel(key) {
  return ORDER_STATUSES.find(s => s.key === key) || { label: key, icon: '📦' };
}

function formatDate(iso) {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

function formatPrice(n) {
  return n.toLocaleString('vi-VN') + '₫';
}

// ---- Render order card ----
function renderOrderCard(order) {
  const st = getStatusLabel(order.status);
  const itemsHtml = order.items.map(it => `
    <div class="order-item">
      <img src="${it.image}" alt="${it.name}" class="order-item-img">
      <div class="order-item-info">
        <div class="order-item-name">${it.name}</div>
        <div class="order-item-meta">${[it.color, it.storage].filter(Boolean).join(' / ')} × ${it.quantity}</div>
        <div class="order-item-price">${formatPrice(it.price * it.quantity)}</div>
      </div>
    </div>
  `).join('');

  const paymentBadge = order.paymentMethod === 'VNPay' ? '🏦 VNPay'
                     : order.paymentMethod === 'MoMo'  ? '📱 MoMo'
                     : order.paymentMethod === 'COD'   ? '💵 COD'
                     : order.paymentMethod || 'COD';

  return `
    <div class="order-card" id="order-${order.id}">
      <div class="order-card-header">
        <div>
          <span class="order-id">Đơn hàng #${order.id}</span>
          <span class="order-date">${formatDate(order.createdAt)}</span>
        </div>
        <span class="order-status-badge status-${order.status}">${st.icon} ${st.label}</span>
      </div>
      <div class="order-items">${itemsHtml}</div>
      <div class="order-card-footer">
        <div class="order-payment">Thanh toán: ${paymentBadge}</div>
        <div class="order-total">Tổng cộng: <strong>${formatPrice(order.total)}</strong></div>
      </div>
      <button class="order-track-btn" onclick="showTracking('${order.id}')">📍 Theo dõi đơn hàng</button>
    </div>
  `;
}

// ---- Render tracking timeline ----
function showTracking(orderId) {
  const order = Orders.getById(orderId);
  if (!order) return;

  const activeSteps = ORDER_STATUSES.filter(s => s.key !== 'cancelled');
  const currentIdx = activeSteps.findIndex(s => s.key === order.status);
  const isCancelled = order.status === 'cancelled';

  const stepsHtml = isCancelled
    ? `<div class="track-cancelled">❌ Đơn hàng đã bị hủy</div>`
    : activeSteps.map((s, i) => {
        const histEntry = order.statusHistory.find(h => h.status === s.key);
        const done = i <= currentIdx;
        return `
          <div class="track-step ${done ? 'done' : ''} ${i === currentIdx ? 'current' : ''}">
            <div class="track-dot">${done ? '✓' : ''}</div>
            <div class="track-info">
              <div class="track-label">${s.icon} ${s.label}</div>
              ${histEntry ? `<div class="track-time">${formatDate(histEntry.time)}</div>` : ''}
              ${histEntry && histEntry.note ? `<div class="track-note" style="font-size:12px;color:#888;">${histEntry.note}</div>` : ''}
            </div>
          </div>
        `;
      }).join('');

  const trackingExtra = order.trackingNumber
    ? `<div class="tracking-address"><strong>Mã vận đơn:</strong> ${order.trackingNumber}</div>`
    : '';

  const modal = document.getElementById('tracking-modal');
  const modalContent = document.getElementById('tracking-content');
  modalContent.innerHTML = `
    <h3 class="tracking-title">Theo dõi đơn hàng #${order.id}</h3>
    <div class="tracking-steps">${stepsHtml}</div>
    ${trackingExtra}
    <div class="tracking-address">
      <strong>Giao đến:</strong> ${order.userInfo.address || 'Không có thông tin'}
    </div>
  `;
  modal.classList.add('open');
}

// ---- API order helpers ----
async function loadOrdersFromApi(token) {
  try {
    const res = await fetch(`${PHONESTORE_API_BASE}/orders`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });
    if (!res.ok) return null;
    const data = await res.json();
    // Normalize API orders to local format
    return data.map(o => {
      // Build status history from API (prefer real history over single entry)
      let statusHistory = [];
      if (o.statusHistories && o.statusHistories.length > 0) {
        statusHistory = o.statusHistories.map(h => ({
          status: h.status.toLowerCase(),
          label: getStatusLabel(h.status.toLowerCase()).label,
          time: h.createdAt,
          note: h.note || '',
        }));
      } else {
        statusHistory = [
          { status: 'pending', label: 'Đã đặt hàng', time: o.orderDate, note: '' },
        ];
      }

      return {
        id: `${o.orderId}`,
        userId: o.userId,
        userInfo: { address: o.shippingAddress || '' },
        items: (o.items || []).map(it => ({
          name: it.productName || '',
          image: it.image || 'https://placehold.co/80x80/1a1a2e/ffffff?text=SP',
          price: it.unitPrice,
          quantity: it.quantity,
          color: it.color || '',
          storage: it.storage || '',
        })),
        paymentMethod: o.paymentMethod || 'COD',
        status: (o.status || 'pending').toLowerCase(),
        statusHistory,
        trackingNumber: o.trackingNumber || '',
        total: o.totalAmount || 0,
        createdAt: o.orderDate,
      };
    });
  } catch (e) {
    return null;
  }
}

// ---- Page init ----
document.addEventListener('DOMContentLoaded', async () => {
  const user = Auth.getCurrentUser();
  const token = Auth.getToken();
  const loginPrompt = document.getElementById('orders-login-prompt');
  const ordersSection = document.getElementById('orders-section');
  const ordersList = document.getElementById('orders-list');

  if (!user) {
    if (loginPrompt) loginPrompt.style.display = '';
    if (ordersSection) ordersSection.style.display = 'none';
    return;
  }

  if (loginPrompt) loginPrompt.style.display = 'none';
  if (ordersSection) ordersSection.style.display = '';

  let userOrders = [];

  // Try loading from API first
  if (token && typeof PHONESTORE_API_BASE !== 'undefined') {
    const apiOrders = await loadOrdersFromApi(token);
    if (apiOrders && apiOrders.length > 0) {
      userOrders = apiOrders.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
    }
  }

  // Fallback to localStorage
  if (userOrders.length === 0) {
    Orders.seedDemoOrders(user.id);
    userOrders = Orders.getUserOrders(user.id)
      .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
  }

  if (ordersList) {
    if (userOrders.length === 0) {
      ordersList.innerHTML = `
        <div class="orders-empty">
          <div class="orders-empty-icon">📦</div>
          <div>Bạn chưa có đơn hàng nào.</div>
          <a href="products.html" class="btn-primary" style="margin-top:16px;display:inline-block;">Mua sắm ngay</a>
        </div>
      `;
    } else {
      ordersList.innerHTML = userOrders.map(renderOrderCard).join('');
    }
  }

  // Search by order ID
  const searchInput = document.getElementById('order-search-input');
  const searchBtn = document.getElementById('order-search-btn');

  function doSearch() {
    const q = searchInput?.value.trim().toLowerCase();
    document.querySelectorAll('.order-card').forEach(card => {
      card.style.display = !q || card.id.toLowerCase().includes(q) ? '' : 'none';
    });
  }

  searchBtn?.addEventListener('click', doSearch);
  searchInput?.addEventListener('keydown', e => { if (e.key === 'Enter') doSearch(); });

  // Modal close
  const modal = document.getElementById('tracking-modal');
  document.getElementById('tracking-modal-close')?.addEventListener('click', () => {
    modal.classList.remove('open');
  });
  modal?.addEventListener('click', (e) => {
    if (e.target === modal) modal.classList.remove('open');
  });
});
