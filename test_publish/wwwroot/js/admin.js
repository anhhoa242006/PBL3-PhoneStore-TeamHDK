// =====================
// ADMIN.JS - Frontend-first admin page controller
// =====================

(function initAdminPage() {
  const PAGE_SIZE = 8;
  const STATUS_META = {
    Pending: { label: 'Chờ xử lý', className: 'pending' },
    Confirmed: { label: 'Đã xác nhận', className: 'confirmed' },
    Shipping: { label: 'Đang giao', className: 'shipping' },
    Delivered: { label: 'Đã giao', className: 'delivered' },
    Cancelled: { label: 'Đã hủy', className: 'cancelled' },
  };

  const uiState = {
    tab: 'dashboard',
    products: { page: 1, search: '', category: 'all', brand: 'all' },
    orders: { page: 1, search: '', status: 'all' },
    users: { page: 1, search: '' },
    banners: { page: 1, search: '' },
    coupons: { page: 1, search: '' },
    reviews: { page: 1, search: '', status: 'all' },
  };

  let dataAccess = null;
  let catalogContext = { categories: [], brands: [], products: [] };

  function byId(id) {
    return document.getElementById(id);
  }

  function escapeHtml(value) {
    return String(value ?? '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  function sanitizeUrl(value) {
    const raw = String(value || '').trim();
    if (!raw) return 'https://placehold.co/80x80/f2f2f2/666?text=No+Image';
    if (/^(https?:\/\/|data:image\/|\.\/|\.\.\/|\/)/i.test(raw)) return raw;
    if (/^[a-zA-Z0-9_./%-]+$/.test(raw)) return raw;
    return 'https://placehold.co/80x80/f2f2f2/666?text=No+Image';
  }

  function formatPrice(value) {
    return Number(value || 0).toLocaleString('vi-VN') + '₫';
  }

  function formatDate(value) {
    if (!value) return '—';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '—';
    return date.toLocaleDateString('vi-VN');
  }

  function showToast(message, type = 'success') {
    const el = byId('admin-toast');
    if (!el) return;
    el.textContent = message;
    el.className = `admin-toast show ${type}`;
    setTimeout(() => {
      el.classList.remove('show');
    }, 2200);
  }

  function renderPagination(containerId, section, page, totalPages) {
    const container = byId(containerId);
    if (!container) return;

    if (totalPages <= 1) {
      container.innerHTML = '';
      return;
    }

    const start = Math.max(1, page - 2);
    const end = Math.min(totalPages, start + 4);

    const buttons = [];
    buttons.push(`<button type="button" class="admin-page-btn" data-page-for="${section}" data-page="${Math.max(1, page - 1)}" ${page === 1 ? 'disabled' : ''}>‹</button>`);

    for (let i = start; i <= end; i += 1) {
      buttons.push(`<button type="button" class="admin-page-btn ${i === page ? 'active' : ''}" data-page-for="${section}" data-page="${i}">${i}</button>`);
    }

    buttons.push(`<button type="button" class="admin-page-btn" data-page-for="${section}" data-page="${Math.min(totalPages, page + 1)}" ${page === totalPages ? 'disabled' : ''}>›</button>`);

    container.innerHTML = buttons.join('');
  }

  function activateTab(tab) {
    uiState.tab = tab;

    document.querySelectorAll('.admin-tab').forEach((btn) => {
      btn.classList.toggle('active', btn.dataset.tab === tab);
    });

    document.querySelectorAll('.admin-panel').forEach((panel) => {
      panel.classList.toggle('active', panel.dataset.panel === tab);
    });
  }

  function getCurrentUserLabel() {
    if (!window.Auth || typeof window.Auth.getCurrentUser !== 'function') {
      return 'Chế độ quản trị cục bộ';
    }

    const user = window.Auth.getCurrentUser();
    if (!user) return 'Khách quản trị (mock mode)';

    const role = user.role ? ` · ${user.role}` : '';
    return `${user.fullName}${role}`;
  }

  function setModeBadge() {
    const badge = byId('admin-mode-badge');
    if (!badge || !dataAccess) return;

    const isMock = dataAccess.mode !== 'api' || window.ADMIN_USE_MOCK;
    badge.textContent = isMock ? 'Mock Data Mode' : 'API Mode';
    badge.classList.toggle('mock', isMock);
    badge.classList.toggle('api', !isMock);
  }

  function updateLastSyncText() {
    const el = byId('admin-last-sync');
    if (!el) return;
    el.textContent = `Cập nhật lúc: ${new Date().toLocaleTimeString('vi-VN')}`;
  }

  async function renderSummary() {
    const stats = await dataAccess.getStats();
    const summaryEl = byId('admin-summary');
    if (!summaryEl) return;

    summaryEl.innerHTML = `
      <div class="admin-card">
        <div class="admin-card-label">Sản phẩm</div>
        <div class="admin-card-value">${Number(stats.totalProducts || 0).toLocaleString('vi-VN')}</div>
      </div>
      <div class="admin-card">
        <div class="admin-card-label">Đơn hàng</div>
        <div class="admin-card-value">${Number(stats.totalOrders || 0).toLocaleString('vi-VN')}</div>
      </div>
      <div class="admin-card">
        <div class="admin-card-label">Khách hàng</div>
        <div class="admin-card-value">${Number(stats.totalUsers || 0).toLocaleString('vi-VN')}</div>
      </div>
      <div class="admin-card">
        <div class="admin-card-label">Doanh thu</div>
        <div class="admin-card-value">${formatPrice(stats.totalRevenue || 0)}</div>
      </div>
      <div class="admin-card">
        <div class="admin-card-label">Chờ xử lý</div>
        <div class="admin-card-value" style="color:#f59e0b">${Number(stats.pendingOrders || 0).toLocaleString('vi-VN')}</div>
      </div>
      <div class="admin-card">
        <div class="admin-card-label">Sắp hết hàng</div>
        <div class="admin-card-value" style="color:#ef4444">${Number(stats.lowStockVariants || 0).toLocaleString('vi-VN')}</div>
      </div>
    `;
  }

  async function renderDashboardInsights() {
    const data = await dataAccess.getDashboardData();

    const topBrandsEl = byId('dashboard-top-brands');
    if (topBrandsEl) {
      const items = data.topBrands || [];
      topBrandsEl.innerHTML = items.length
        ? items.map((item) => `
            <div class="admin-list-row">
              <span>${escapeHtml(item.brand)}</span>
              <strong>${Number(item.count || 0).toLocaleString('vi-VN')} SP</strong>
            </div>
          `).join('')
        : '<div class="admin-empty">Chưa có dữ liệu thương hiệu.</div>';
    }

    const statusEl = byId('dashboard-order-status');
    if (statusEl) {
      const items = data.orderStatus || [];
      statusEl.innerHTML = items.length
        ? items.map((item) => {
            const meta = STATUS_META[item.status] || { label: item.status, className: 'pending' };
            return `
              <div class="admin-list-row">
                <span class="admin-status-pill ${meta.className}">${escapeHtml(meta.label)}</span>
                <strong>${Number(item.count || 0).toLocaleString('vi-VN')} đơn</strong>
              </div>
            `;
          }).join('')
        : '<div class="admin-empty">Chưa có dữ liệu trạng thái đơn hàng.</div>';
    }

    const lowStockEl = byId('dashboard-low-stock');
    if (lowStockEl) {
      const items = data.lowStock || [];
      lowStockEl.innerHTML = items.length
        ? items.map((item) => `
            <div class="admin-list-row">
              <span>${escapeHtml(item.name)}</span>
              <strong style="color:#ef4444">${Number(item.stock || 0)} còn lại</strong>
            </div>
          `).join('')
        : '<div class="admin-empty">Không có sản phẩm sắp hết hàng.</div>';
    }
  }

  function fillProductFilters() {
    const state = dataAccess.getState();
    const categorySelect = byId('products-category-filter');
    const brandSelect = byId('products-brand-filter');

    if (!categorySelect || !brandSelect) return;

    const categories = (state.categories || []).slice().sort((a, b) => String(a).localeCompare(String(b), 'vi'));
    const brands = (state.brands || []).slice().sort((a, b) => String(a).localeCompare(String(b), 'vi'));

    categorySelect.innerHTML = `<option value="all">Tất cả danh mục</option>${categories.map((item) => `<option value="${escapeHtml(item)}">${escapeHtml(item)}</option>`).join('')}`;
    brandSelect.innerHTML = `<option value="all">Tất cả thương hiệu</option>${brands.map((item) => `<option value="${escapeHtml(item)}">${escapeHtml(item)}</option>`).join('')}`;

    categorySelect.value = uiState.products.category;
    brandSelect.value = uiState.products.brand;
  }

  async function renderProducts() {
    fillProductFilters();

    const result = await dataAccess.listProducts({
      search: uiState.products.search,
      category: uiState.products.category,
      brand: uiState.products.brand,
      page: uiState.products.page,
      pageSize: PAGE_SIZE,
    });

    const totalEl = byId('products-total');
    const bodyEl = byId('products-body');

    if (totalEl) totalEl.textContent = `Tổng: ${Number(result.total || 0).toLocaleString('vi-VN')} sản phẩm`;

    if (bodyEl) {
      if (!result.items.length) {
        bodyEl.innerHTML = '<tr><td colspan="7" class="admin-empty">Không có sản phẩm phù hợp.</td></tr>';
      } else {
        bodyEl.innerHTML = result.items.map((product) => `
          <tr>
            <td>
              <div class="admin-product">
                <img src="${escapeHtml(sanitizeUrl(product.image))}" alt="${escapeHtml(product.name)}" loading="lazy">
                <div>
                  <div class="admin-product-name">${escapeHtml(product.name)}</div>
                  <div class="admin-product-meta">${escapeHtml(product.slug || '')}</div>
                </div>
              </div>
            </td>
            <td>${escapeHtml(product.categoryName || product.category || '—')}</td>
            <td>${escapeHtml(product.brand || '—')}</td>
            <td>${formatPrice(product.price)}</td>
            <td>${Number(product.stock || 0).toLocaleString('vi-VN')}</td>
            <td><span class="admin-status-pill ${product.isActive ? 'delivered' : 'cancelled'}">${product.isActive ? 'Đang bán' : 'Ngừng bán'}</span></td>
            <td>
              <div class="admin-row-actions">
                <button type="button" class="admin-mini-btn" data-action="edit-product" data-id="${product.id}">Sửa</button>
                <button type="button" class="admin-mini-btn danger" data-action="delete-product" data-id="${product.id}">Xóa</button>
              </div>
            </td>
          </tr>
        `).join('');
      }
    }

    renderPagination('products-pagination', 'products', result.page, result.totalPages);
  }

  async function renderOrders() {
    const result = await dataAccess.listOrders({
      search: uiState.orders.search,
      status: uiState.orders.status,
      page: uiState.orders.page,
      pageSize: PAGE_SIZE,
    });

    const totalEl = byId('orders-total');
    const bodyEl = byId('orders-body');

    if (totalEl) totalEl.textContent = `Tổng: ${Number(result.total || 0).toLocaleString('vi-VN')} đơn hàng`;

    if (bodyEl) {
      if (!result.items.length) {
        bodyEl.innerHTML = '<tr><td colspan="8" class="admin-empty">Không có đơn hàng phù hợp.</td></tr>';
      } else {
        bodyEl.innerHTML = result.items.map((order) => {
          const meta = STATUS_META[order.status] || { label: order.status || 'Unknown', className: 'pending' };
          return `
            <tr>
              <td>#${escapeHtml(order.orderId)}</td>
              <td>
                <div>${escapeHtml(order.customerName || 'Khách')}</div>
                <div class="admin-product-meta">${escapeHtml(order.customerEmail || '')}</div>
              </td>
              <td>${formatDate(order.orderDate)}</td>
              <td>${formatPrice(order.totalAmount)}</td>
              <td>${escapeHtml(order.paymentMethod || 'COD')}</td>
              <td>${Number(order.itemCount || 0)}</td>
              <td>
                <span class="admin-status-pill ${meta.className}">${escapeHtml(meta.label)}</span>
              </td>
              <td>
                <select class="admin-select order-status-select" data-order-id="${order.orderId}">
                  ${(window.ADMIN_ORDER_STATUSES || Object.keys(STATUS_META)).map((status) => `
                    <option value="${status}" ${status === order.status ? 'selected' : ''}>${escapeHtml((STATUS_META[status] || {}).label || status)}</option>
                  `).join('')}
                </select>
              </td>
            </tr>
          `;
        }).join('');
      }
    }

    renderPagination('orders-pagination', 'orders', result.page, result.totalPages);
  }

  async function renderUsers() {
    const result = await dataAccess.listUsers({
      search: uiState.users.search,
      page: uiState.users.page,
      pageSize: PAGE_SIZE,
    });

    const totalEl = byId('users-total');
    const bodyEl = byId('users-body');

    if (totalEl) totalEl.textContent = `Tổng: ${Number(result.total || 0).toLocaleString('vi-VN')} người dùng`;

    if (bodyEl) {
      bodyEl.innerHTML = result.items.length
        ? result.items.map((user) => `
            <tr>
              <td>${escapeHtml(user.fullName || '—')}</td>
              <td>${escapeHtml(user.email || '—')}</td>
              <td>${escapeHtml(user.phoneNumber || '—')}</td>
              <td>${escapeHtml(user.role || 'Customer')}</td>
              <td><span class="admin-status-pill ${user.isActive ? 'delivered' : 'cancelled'}">${user.isActive ? 'Active' : 'Inactive'}</span></td>
              <td>${formatDate(user.createdAt)}</td>
            </tr>
          `).join('')
        : '<tr><td colspan="6" class="admin-empty">Không có người dùng phù hợp.</td></tr>';
    }

    renderPagination('users-pagination', 'users', result.page, result.totalPages);
  }

  async function renderBanners() {
    const result = await dataAccess.listBanners({
      search: uiState.banners.search,
      page: uiState.banners.page,
      pageSize: PAGE_SIZE,
    });

    const totalEl = byId('banners-total');
    const bodyEl = byId('banners-body');

    if (totalEl) totalEl.textContent = `Tổng: ${Number(result.total || 0).toLocaleString('vi-VN')} banner`;

    if (bodyEl) {
      bodyEl.innerHTML = result.items.length
        ? result.items.map((banner) => `
            <tr>
              <td>${escapeHtml(banner.title || '—')}</td>
              <td><a href="${escapeHtml(sanitizeUrl(banner.imageUrl))}" target="_blank" rel="noopener">Xem ảnh</a></td>
              <td>${escapeHtml(banner.linkUrl || '—')}</td>
              <td>${Number(banner.displayOrder || 0)}</td>
              <td><span class="admin-status-pill ${banner.isActive ? 'delivered' : 'cancelled'}">${banner.isActive ? 'Hiển thị' : 'Ẩn'}</span></td>
              <td>
                <div class="admin-row-actions">
                  <button type="button" class="admin-mini-btn" data-action="edit-banner" data-id="${banner.id}">Sửa</button>
                  <button type="button" class="admin-mini-btn danger" data-action="delete-banner" data-id="${banner.id}">Xóa</button>
                </div>
              </td>
            </tr>
          `).join('')
        : '<tr><td colspan="6" class="admin-empty">Không có banner phù hợp.</td></tr>';
    }

    renderPagination('banners-pagination', 'banners', result.page, result.totalPages);
  }

  async function renderCoupons() {
    const result = await dataAccess.listCoupons({
      search: uiState.coupons.search,
      page: uiState.coupons.page,
      pageSize: PAGE_SIZE,
    });

    const totalEl = byId('coupons-total');
    const bodyEl = byId('coupons-body');

    if (totalEl) totalEl.textContent = `Tổng: ${Number(result.total || 0).toLocaleString('vi-VN')} mã`;

    if (bodyEl) {
      bodyEl.innerHTML = result.items.length
        ? result.items.map((coupon) => `
            <tr>
              <td>${escapeHtml(coupon.code || '—')}</td>
              <td>${escapeHtml(coupon.description || '')}</td>
              <td>${coupon.type === 'amount' ? 'Theo tiền' : 'Theo %'}</td>
              <td>${coupon.type === 'amount' ? formatPrice(coupon.value) : `${Number(coupon.value || 0)}%`}</td>
              <td>${escapeHtml(coupon.startDate || '—')} → ${escapeHtml(coupon.endDate || '—')}</td>
              <td><span class="admin-status-pill ${coupon.isActive ? 'delivered' : 'cancelled'}">${coupon.isActive ? 'Đang chạy' : 'Tạm dừng'}</span></td>
              <td>
                <div class="admin-row-actions">
                  <button type="button" class="admin-mini-btn" data-action="edit-coupon" data-id="${coupon.id}">Sửa</button>
                  <button type="button" class="admin-mini-btn danger" data-action="delete-coupon" data-id="${coupon.id}">Xóa</button>
                </div>
              </td>
            </tr>
          `).join('')
        : '<tr><td colspan="7" class="admin-empty">Không có mã giảm giá phù hợp.</td></tr>';
    }

    renderPagination('coupons-pagination', 'coupons', result.page, result.totalPages);
  }

  async function renderReviews() {
    const result = await dataAccess.listReviews({
      search: uiState.reviews.search,
      status: uiState.reviews.status,
      page: uiState.reviews.page,
      pageSize: PAGE_SIZE,
    });

    const totalEl = byId('reviews-total');
    const bodyEl = byId('reviews-body');

    if (totalEl) totalEl.textContent = `Tổng: ${Number(result.total || 0).toLocaleString('vi-VN')} đánh giá`;

    if (bodyEl) {
      bodyEl.innerHTML = result.items.length
        ? result.items.map((review) => `
            <tr>
              <td>${escapeHtml(review.productName || `#${review.productId}`)}</td>
              <td>${escapeHtml(review.userName || 'Ẩn danh')}</td>
              <td>${'★'.repeat(Number(review.rating || 0))}</td>
              <td>${escapeHtml(review.comment || '')}</td>
              <td><span class="admin-status-pill ${review.isApproved ? 'delivered' : 'pending'}">${review.isApproved ? 'Đã duyệt' : 'Chờ duyệt'}</span></td>
              <td>${formatDate(review.createdAt)}</td>
              <td>
                <div class="admin-row-actions">
                  ${review.isApproved ? '' : `<button type="button" class="admin-mini-btn" data-action="approve-review" data-id="${review.reviewId}">Duyệt</button>`}
                  <button type="button" class="admin-mini-btn danger" data-action="delete-review" data-id="${review.reviewId}">Xóa</button>
                </div>
              </td>
            </tr>
          `).join('')
        : '<tr><td colspan="7" class="admin-empty">Không có đánh giá phù hợp.</td></tr>';
    }

    renderPagination('reviews-pagination', 'reviews', result.page, result.totalPages);
  }

  async function refreshEverything() {
    await renderSummary();
    await renderDashboardInsights();
    await Promise.all([
      renderProducts(),
      renderOrders(),
      renderUsers(),
      renderBanners(),
      renderCoupons(),
      renderReviews(),
    ]);
    updateLastSyncText();
  }

  function openProductForm(product) {
    const form = byId('product-form');
    if (!form) return;

    form.classList.remove('hidden');
    form.reset();

    byId('product-id').value = product?.id || '';
    byId('product-name').value = product?.name || '';
    byId('product-slug').value = product?.slug || '';
    byId('product-brand').value = product?.brand || '';
    byId('product-category-name').value = product?.categoryName || '';
    byId('product-price').value = product?.price || 0;
    byId('product-stock').value = product?.stock || 0;
    byId('product-image').value = product?.image || '';
    byId('product-active').checked = product?.isActive !== false;
  }

  function closeProductForm() {
    const form = byId('product-form');
    if (!form) return;
    form.classList.add('hidden');
    form.reset();
  }

  function openBannerForm(banner) {
    const form = byId('banner-form');
    if (!form) return;

    form.classList.remove('hidden');
    form.reset();

    byId('banner-id').value = banner?.id || '';
    byId('banner-title').value = banner?.title || '';
    byId('banner-image').value = banner?.imageUrl || '';
    byId('banner-link').value = banner?.linkUrl || '';
    byId('banner-order').value = banner?.displayOrder || 0;
    byId('banner-active').checked = banner?.isActive !== false;
  }

  function closeBannerForm() {
    const form = byId('banner-form');
    if (!form) return;
    form.classList.add('hidden');
    form.reset();
  }

  function openCouponForm(coupon) {
    const form = byId('coupon-form');
    if (!form) return;

    form.classList.remove('hidden');
    form.reset();

    byId('coupon-id').value = coupon?.id || '';
    byId('coupon-code').value = coupon?.code || '';
    byId('coupon-description').value = coupon?.description || '';
    byId('coupon-type').value = coupon?.type || 'percent';
    byId('coupon-value').value = coupon?.value || 0;
    byId('coupon-start').value = coupon?.startDate || '';
    byId('coupon-end').value = coupon?.endDate || '';
    byId('coupon-active').checked = coupon?.isActive !== false;
  }

  function closeCouponForm() {
    const form = byId('coupon-form');
    if (!form) return;
    form.classList.add('hidden');
    form.reset();
  }

  async function handleActionClick(button) {
    const action = button.dataset.action;
    const id = Number(button.dataset.id || 0);
    const currentState = dataAccess.getState();

    if (action === 'edit-product') {
      const product = currentState.products.find((item) => Number(item.id) === id);
      openProductForm(product);
      return;
    }

    if (action === 'delete-product') {
      if (!confirm('Bạn chắc chắn muốn xóa sản phẩm này?')) return;
      await dataAccess.deleteProduct(id);
      showToast('Đã xóa sản phẩm.');
      await refreshEverything();
      return;
    }

    if (action === 'edit-banner') {
      const banner = currentState.banners.find((item) => Number(item.id) === id);
      openBannerForm(banner);
      return;
    }

    if (action === 'delete-banner') {
      if (!confirm('Bạn chắc chắn muốn xóa banner này?')) return;
      await dataAccess.deleteBanner(id);
      showToast('Đã xóa banner.');
      await refreshEverything();
      return;
    }

    if (action === 'edit-coupon') {
      const coupon = currentState.coupons.find((item) => Number(item.id) === id);
      openCouponForm(coupon);
      return;
    }

    if (action === 'delete-coupon') {
      if (!confirm('Bạn chắc chắn muốn xóa coupon này?')) return;
      await dataAccess.deleteCoupon(id);
      showToast('Đã xóa coupon.');
      await refreshEverything();
      return;
    }

    if (action === 'approve-review') {
      await dataAccess.approveReview(id);
      showToast('Đã duyệt đánh giá.');
      await refreshEverything();
      return;
    }

    if (action === 'delete-review') {
      if (!confirm('Bạn chắc chắn muốn xóa đánh giá này?')) return;
      await dataAccess.deleteReview(id);
      showToast('Đã xóa đánh giá.');
      await refreshEverything();
    }
  }

  function bindEvents() {
    byId('admin-tabs')?.addEventListener('click', (event) => {
      const button = event.target.closest('.admin-tab');
      if (!button) return;
      activateTab(button.dataset.tab || 'dashboard');
    });

    document.addEventListener('click', async (event) => {
      const pageButton = event.target.closest('.admin-page-btn');
      if (pageButton) {
        const section = pageButton.dataset.pageFor;
        const page = Number(pageButton.dataset.page || 1);
        if (!section || !uiState[section]) return;
        uiState[section].page = page;

        if (section === 'products') await renderProducts();
        if (section === 'orders') await renderOrders();
        if (section === 'users') await renderUsers();
        if (section === 'banners') await renderBanners();
        if (section === 'coupons') await renderCoupons();
        if (section === 'reviews') await renderReviews();
        return;
      }

      const actionButton = event.target.closest('[data-action]');
      if (actionButton) {
        await handleActionClick(actionButton);
      }
    });

    byId('products-search')?.addEventListener('input', async (event) => {
      uiState.products.search = event.target.value || '';
      uiState.products.page = 1;
      await renderProducts();
    });

    byId('products-category-filter')?.addEventListener('change', async (event) => {
      uiState.products.category = event.target.value || 'all';
      uiState.products.page = 1;
      await renderProducts();
    });

    byId('products-brand-filter')?.addEventListener('change', async (event) => {
      uiState.products.brand = event.target.value || 'all';
      uiState.products.page = 1;
      await renderProducts();
    });

    byId('orders-search')?.addEventListener('input', async (event) => {
      uiState.orders.search = event.target.value || '';
      uiState.orders.page = 1;
      await renderOrders();
    });

    byId('orders-status-filter')?.addEventListener('change', async (event) => {
      uiState.orders.status = event.target.value || 'all';
      uiState.orders.page = 1;
      await renderOrders();
    });

    byId('users-search')?.addEventListener('input', async (event) => {
      uiState.users.search = event.target.value || '';
      uiState.users.page = 1;
      await renderUsers();
    });

    byId('banners-search')?.addEventListener('input', async (event) => {
      uiState.banners.search = event.target.value || '';
      uiState.banners.page = 1;
      await renderBanners();
    });

    byId('coupons-search')?.addEventListener('input', async (event) => {
      uiState.coupons.search = event.target.value || '';
      uiState.coupons.page = 1;
      await renderCoupons();
    });

    byId('reviews-search')?.addEventListener('input', async (event) => {
      uiState.reviews.search = event.target.value || '';
      uiState.reviews.page = 1;
      await renderReviews();
    });

    byId('reviews-status-filter')?.addEventListener('change', async (event) => {
      uiState.reviews.status = event.target.value || 'all';
      uiState.reviews.page = 1;
      await renderReviews();
    });

    byId('orders-body')?.addEventListener('change', async (event) => {
      if (!event.target.classList.contains('order-status-select')) return;

      const orderId = Number(event.target.dataset.orderId || 0);
      const status = event.target.value;
      const result = await dataAccess.updateOrderStatus(orderId, status);
      if (result.success) {
        showToast(`Đã cập nhật đơn #${orderId}.`);
        await refreshEverything();
      } else {
        showToast(result.message || 'Cập nhật trạng thái thất bại.', 'error');
      }
    });

    byId('product-form-toggle')?.addEventListener('click', () => openProductForm(null));
    byId('product-form-cancel')?.addEventListener('click', closeProductForm);

    byId('product-form')?.addEventListener('submit', async (event) => {
      event.preventDefault();

      const payload = {
        id: Number(byId('product-id').value || 0),
        name: byId('product-name').value,
        slug: byId('product-slug').value,
        brand: byId('product-brand').value,
        categoryName: byId('product-category-name').value,
        category: (byId('product-category-name').value || 'khac').toLowerCase().replace(/\s+/g, '-'),
        price: Number(byId('product-price').value || 0),
        stock: Number(byId('product-stock').value || 0),
        image: byId('product-image').value,
        isActive: byId('product-active').checked,
      };

      await dataAccess.upsertProduct(payload);
      closeProductForm();
      showToast(payload.id ? 'Đã cập nhật sản phẩm.' : 'Đã thêm sản phẩm mới.');
      await refreshEverything();
    });

    byId('banner-form-toggle')?.addEventListener('click', () => openBannerForm(null));
    byId('banner-form-cancel')?.addEventListener('click', closeBannerForm);

    byId('banner-form')?.addEventListener('submit', async (event) => {
      event.preventDefault();

      const payload = {
        id: Number(byId('banner-id').value || 0),
        title: byId('banner-title').value,
        imageUrl: byId('banner-image').value,
        linkUrl: byId('banner-link').value,
        displayOrder: Number(byId('banner-order').value || 0),
        isActive: byId('banner-active').checked,
      };

      await dataAccess.upsertBanner(payload);
      closeBannerForm();
      showToast(payload.id ? 'Đã cập nhật banner.' : 'Đã thêm banner mới.');
      await refreshEverything();
    });

    byId('coupon-form-toggle')?.addEventListener('click', () => openCouponForm(null));
    byId('coupon-form-cancel')?.addEventListener('click', closeCouponForm);

    byId('coupon-form')?.addEventListener('submit', async (event) => {
      event.preventDefault();

      const payload = {
        id: Number(byId('coupon-id').value || 0),
        code: byId('coupon-code').value,
        description: byId('coupon-description').value,
        type: byId('coupon-type').value,
        value: Number(byId('coupon-value').value || 0),
        startDate: byId('coupon-start').value,
        endDate: byId('coupon-end').value,
        isActive: byId('coupon-active').checked,
      };

      await dataAccess.upsertCoupon(payload);
      closeCouponForm();
      showToast(payload.id ? 'Đã cập nhật coupon.' : 'Đã thêm coupon mới.');
      await refreshEverything();
    });

    byId('admin-reset-data')?.addEventListener('click', async () => {
      if (!confirm('Bạn muốn reset toàn bộ dữ liệu admin local về mặc định?')) return;
      await dataAccess.reset({ catalog: catalogContext });
      closeProductForm();
      closeBannerForm();
      closeCouponForm();
      showToast('Đã reset dữ liệu admin local.');
      await refreshEverything();
    });
  }

  async function bootstrap() {
    if (typeof window.loadCatalog === 'function') {
      try {
        await window.loadCatalog();
      } catch {
        // Continue with local seed data.
      }
    }

    catalogContext = {
      categories: typeof categories !== 'undefined' ? categories : [],
      brands: typeof brands !== 'undefined' ? brands : [],
      products: typeof products !== 'undefined' ? products : [],
    };

    dataAccess = window.AdminDataAccess || window.AdminStore;
    if (!dataAccess || typeof dataAccess.init !== 'function') {
      showToast('Không khởi tạo được dữ liệu admin.', 'error');
      return;
    }

    await dataAccess.init({ catalog: catalogContext });

    const userEl = byId('admin-user');
    if (userEl) {
      userEl.textContent = getCurrentUserLabel();
    }

    setModeBadge();
    activateTab('dashboard');
    bindEvents();
    await refreshEverything();
  }

  document.addEventListener('DOMContentLoaded', bootstrap);
  
  // Vô hiệu hóa kiểm tra quyền trên Frontend (JS) 
  // vì Backend ASP.NET Core đã dùng [Authorize(Roles="Admin")] rồi.
  /*
  const currentUser = window.Auth?.getCurrentUser();
  if (!currentUser || (currentUser.role !== 'Admin' && currentUser.role !== 'Staff')) {
      alert('Bạn không có quyền truy cập trang này!');
      window.location.href = 'index.html';
      return;
  }
  */
})();
