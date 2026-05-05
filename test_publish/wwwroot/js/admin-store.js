// =====================
// ADMIN-STORE.JS - Frontend-first admin data layer
// =====================

(function initAdminStore() {
  const STORAGE_KEY = 'phonestore_admin_store_v1';
  const STORE_VERSION = 1;
  const ORDER_STATUSES = ['Pending', 'Confirmed', 'Shipping', 'Delivered', 'Cancelled'];
  const DEFAULT_PAGE_SIZE = 8;

  let state = null;

  function deepClone(value) {
    return JSON.parse(JSON.stringify(value));
  }

  function toNumber(value, fallback = 0) {
    const n = Number(value);
    return Number.isFinite(n) ? n : fallback;
  }

  function asString(value, fallback = '') {
    const s = String(value ?? '').trim();
    return s || fallback;
  }

  function readStore() {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const parsed = JSON.parse(raw);
      if (!parsed || parsed._version !== STORE_VERSION) return null;

      const requiredArrays = ['categories', 'brands', 'products', 'orders', 'users', 'banners', 'coupons', 'reviews'];
      const valid = requiredArrays.every((key) => Array.isArray(parsed[key]));
      return valid ? parsed : null;
    } catch {
      return null;
    }
  }

  function writeStore() {
    if (!state) return;
    state.updatedAt = new Date().toISOString();
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  }

  function uniq(values) {
    return Array.from(new Set(values.filter(Boolean)));
  }

  function sumStock(product) {
    if (Array.isArray(product.variants) && product.variants.length) {
      return product.variants.reduce((sum, item) => sum + toNumber(item.stock ?? item.stockQuantity, 0), 0);
    }
    return toNumber(product.stock, 0);
  }

  function normalizeProducts(catalogProducts) {
    const list = Array.isArray(catalogProducts) ? catalogProducts : [];

    return list.map((item, index) => {
      const id = toNumber(item.id ?? item.productId ?? item.ProductID, index + 1);
      const categoryName = asString(item.categoryName || item.CategoryName || item.category || item.Category || 'Khác');
      const brand = asString(item.brand || item.BrandName || item.brandName || 'Unknown');
      const productName = asString(item.name || item.productName || item.ProductName || `Sản phẩm ${id}`);
      const slug = asString(item.slug || item.Slug || productName.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, ''), `product-${id}`);

      return {
        id,
        name: productName,
        slug,
        brand,
        categoryName,
        category: asString(item.category || item.categorySlug || item.CategorySlug || categoryName.toLowerCase()),
        price: toNumber(item.price, 0),
        stock: sumStock(item),
        image: asString(item.image || (Array.isArray(item.images) ? item.images[0] : ''), 'https://placehold.co/80x80/f2f2f2/666?text=No+Image'),
        isActive: item.isActive !== false,
        createdAt: asString(item.createdAt, new Date(Date.now() - index * 86400000).toISOString()),
      };
    });
  }

  function generateUsers() {
    return [
      {
        userId: 1,
        fullName: 'Admin PhoneStore',
        email: 'admin@phonestore.vn',
        phoneNumber: '0900000000',
        role: 'Admin',
        isActive: true,
        createdAt: '2025-01-01T08:00:00.000Z',
      },
      {
        userId: 2,
        fullName: 'Staff PhoneStore',
        email: 'staff@phonestore.vn',
        phoneNumber: '0900000001',
        role: 'Staff',
        isActive: true,
        createdAt: '2025-01-02T08:00:00.000Z',
      },
      {
        userId: 3,
        fullName: 'Nguyen Van A',
        email: 'nguyenvana@gmail.com',
        phoneNumber: '0901111222',
        role: 'Customer',
        isActive: true,
        createdAt: '2025-02-10T08:00:00.000Z',
      },
      {
        userId: 4,
        fullName: 'Tran Thi B',
        email: 'tranthib@gmail.com',
        phoneNumber: '0903333444',
        role: 'Customer',
        isActive: true,
        createdAt: '2025-02-15T08:00:00.000Z',
      },
      {
        userId: 5,
        fullName: 'Le Van C',
        email: 'levanc@gmail.com',
        phoneNumber: '0907777888',
        role: 'Customer',
        isActive: false,
        createdAt: '2025-03-01T08:00:00.000Z',
      },
    ];
  }

  function generateOrders(products) {
    const safeProducts = Array.isArray(products) ? products : [];
    const now = Date.now();
    const paymentMethods = ['COD', 'VNPAY', 'MoMo'];

    if (!safeProducts.length) {
      return [];
    }

    const orders = [];
    for (let i = 0; i < 16; i += 1) {
      const product = safeProducts[i % safeProducts.length];
      const quantity = (i % 3) + 1;
      const status = ORDER_STATUSES[i % ORDER_STATUSES.length];
      orders.push({
        orderId: 2000 + i,
        customerName: `Khách hàng ${i + 1}`,
        customerEmail: `khach${i + 1}@gmail.com`,
        orderDate: new Date(now - i * 3600000 * 18).toISOString(),
        totalAmount: toNumber(product.price, 0) * quantity,
        paymentMethod: paymentMethods[i % paymentMethods.length],
        status,
        itemCount: quantity,
        shippingAddress: `${100 + i} Nguyễn Văn Linh, Đà Nẵng`,
        trackingNumber: status === 'Shipping' || status === 'Delivered' ? `VN${900000 + i}` : '',
      });
    }

    return orders;
  }

  function generateBanners() {
    return [
      {
        id: 1,
        title: 'Mùa hè rực rỡ - Giảm đến 30%',
        imageUrl: 'https://placehold.co/600x240/d0021b/ffffff?text=SUMMER+SALE',
        linkUrl: 'products.html',
        displayOrder: 1,
        isActive: true,
      },
      {
        id: 2,
        title: 'Flash sale cuối tuần',
        imageUrl: 'https://placehold.co/600x240/0f3460/ffffff?text=FLASH+SALE',
        linkUrl: 'products.html?sort=price-asc',
        displayOrder: 2,
        isActive: true,
      },
      {
        id: 3,
        title: 'Laptop học tập giá tốt',
        imageUrl: 'https://placehold.co/600x240/f39c12/1a1a2e?text=LAPTOP+DEAL',
        linkUrl: 'products.html?cat=laptop',
        displayOrder: 3,
        isActive: false,
      },
    ];
  }

  function generateCoupons() {
    return [
      {
        id: 1,
        code: 'NEW10',
        description: 'Giảm 10% cho khách hàng mới',
        type: 'percent',
        value: 10,
        startDate: '2026-01-01',
        endDate: '2026-12-31',
        isActive: true,
      },
      {
        id: 2,
        code: 'FREESHIP',
        description: 'Giảm 30.000đ phí vận chuyển',
        type: 'amount',
        value: 30000,
        startDate: '2026-01-01',
        endDate: '2026-12-31',
        isActive: true,
      },
      {
        id: 3,
        code: 'VIP200',
        description: 'Giảm 200.000đ cho đơn từ 10 triệu',
        type: 'amount',
        value: 200000,
        startDate: '2026-03-01',
        endDate: '2026-06-30',
        isActive: false,
      },
    ];
  }

  function generateReviews(products) {
    const safeProducts = Array.isArray(products) ? products : [];
    if (!safeProducts.length) return [];

    return safeProducts.slice(0, 10).map((product, index) => ({
      reviewId: 5000 + index,
      productId: product.id,
      productName: product.name,
      userName: `Người dùng ${index + 1}`,
      rating: 3 + (index % 3),
      comment: index % 2 === 0 ? 'Sản phẩm dùng ổn, giao hàng nhanh.' : 'Thiết kế đẹp, cần cải thiện pin.',
      isApproved: index % 3 !== 0,
      createdAt: new Date(Date.now() - index * 86400000).toISOString(),
    }));
  }

  function buildInitialState(catalog) {
    const safeCatalog = catalog || {};
    const normalizedProducts = normalizeProducts(safeCatalog.products || []);

    const fallbackCategories = uniq(normalizedProducts.map((p) => p.categoryName));
    const fallbackBrands = uniq(normalizedProducts.map((p) => p.brand));

    const categories = Array.isArray(safeCatalog.categories) && safeCatalog.categories.length
      ? uniq(safeCatalog.categories.map((item) => item.name || item.categoryName || item.CategoryName || item.id))
      : fallbackCategories;

    const brands = Array.isArray(safeCatalog.brands) && safeCatalog.brands.length
      ? uniq(safeCatalog.brands.map((item) => (typeof item === 'string' ? item : item.brandName || item.BrandName || item.name)))
      : fallbackBrands;

    return {
      _version: STORE_VERSION,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      categories,
      brands,
      products: normalizedProducts,
      orders: generateOrders(normalizedProducts),
      users: generateUsers(),
      banners: generateBanners(),
      coupons: generateCoupons(),
      reviews: generateReviews(normalizedProducts),
    };
  }

  function paginate(items, page = 1, pageSize = DEFAULT_PAGE_SIZE) {
    const total = items.length;
    const size = Math.max(1, toNumber(pageSize, DEFAULT_PAGE_SIZE));
    const totalPages = Math.max(1, Math.ceil(total / size));
    const currentPage = Math.min(Math.max(1, toNumber(page, 1)), totalPages);
    const start = (currentPage - 1) * size;
    return {
      items: items.slice(start, start + size),
      total,
      page: currentPage,
      pageSize: size,
      totalPages,
    };
  }

  function containsText(value, keyword) {
    const source = String(value || '').toLowerCase();
    const key = String(keyword || '').toLowerCase().trim();
    return !key || source.includes(key);
  }

  function ensureState(catalog) {
    if (state) return;
    state = readStore();
    if (!state) {
      state = buildInitialState(catalog);
      writeStore();
    }
  }

  const mockStore = {
    mode: 'mock',

    async init({ catalog } = {}) {
      ensureState(catalog);
      return deepClone(state);
    },

    async reset({ catalog } = {}) {
      state = buildInitialState(catalog);
      writeStore();
      return deepClone(state);
    },

    getState() {
      ensureState({});
      return deepClone(state);
    },

    async getStats() {
      ensureState({});

      const totalProducts = state.products.filter((p) => p.isActive !== false).length;
      const totalOrders = state.orders.length;
      const totalUsers = state.users.filter((u) => u.isActive).length;
      const totalRevenue = state.orders
        .filter((o) => o.status !== 'Cancelled')
        .reduce((sum, item) => sum + toNumber(item.totalAmount, 0), 0);
      const pendingOrders = state.orders.filter((o) => o.status === 'Pending').length;
      const lowStockVariants = state.products.filter((p) => toNumber(p.stock, 0) < 5).length;

      return {
        totalProducts,
        totalOrders,
        totalUsers,
        totalRevenue,
        pendingOrders,
        lowStockVariants,
      };
    },

    async getDashboardData() {
      ensureState({});

      const brandMap = state.products.reduce((acc, product) => {
        const key = product.brand || 'Khác';
        acc[key] = (acc[key] || 0) + 1;
        return acc;
      }, {});

      const orderStatusMap = state.orders.reduce((acc, order) => {
        const key = order.status || 'Pending';
        acc[key] = (acc[key] || 0) + 1;
        return acc;
      }, {});

      const lowStock = state.products
        .filter((p) => toNumber(p.stock, 0) < 5)
        .sort((a, b) => toNumber(a.stock, 0) - toNumber(b.stock, 0))
        .slice(0, 6)
        .map((item) => ({ id: item.id, name: item.name, stock: item.stock }));

      const topBrands = Object.entries(brandMap)
        .map(([brand, count]) => ({ brand, count }))
        .sort((a, b) => b.count - a.count)
        .slice(0, 8);

      const orderStatus = ORDER_STATUSES.map((status) => ({
        status,
        count: orderStatusMap[status] || 0,
      }));

      return { topBrands, orderStatus, lowStock };
    },

    async listProducts({ search = '', category = 'all', brand = 'all', page = 1, pageSize = DEFAULT_PAGE_SIZE } = {}) {
      ensureState({});

      let filtered = state.products.slice();
      if (category !== 'all') {
        filtered = filtered.filter((item) => item.categoryName === category || item.category === category);
      }
      if (brand !== 'all') {
        filtered = filtered.filter((item) => item.brand === brand);
      }
      if (search) {
        filtered = filtered.filter((item) => (
          containsText(item.name, search)
          || containsText(item.slug, search)
          || containsText(item.brand, search)
          || containsText(item.categoryName, search)
        ));
      }

      filtered.sort((a, b) => toNumber(b.id, 0) - toNumber(a.id, 0));
      return paginate(filtered, page, pageSize);
    },

    async upsertProduct(payload) {
      ensureState({});

      const id = toNumber(payload.id, 0);
      const product = {
        id: id || (Math.max(0, ...state.products.map((item) => toNumber(item.id, 0))) + 1),
        name: asString(payload.name, 'Sản phẩm mới'),
        slug: asString(payload.slug, `san-pham-${Date.now()}`),
        brand: asString(payload.brand, 'Unknown'),
        categoryName: asString(payload.categoryName, 'Khác'),
        category: asString(payload.category, asString(payload.categoryName, 'khac').toLowerCase().replace(/\s+/g, '-')),
        price: Math.max(0, toNumber(payload.price, 0)),
        stock: Math.max(0, toNumber(payload.stock, 0)),
        image: asString(payload.image, 'https://placehold.co/80x80/f2f2f2/666?text=No+Image'),
        isActive: payload.isActive !== false,
        createdAt: asString(payload.createdAt, new Date().toISOString()),
      };

      const index = state.products.findIndex((item) => item.id === product.id);
      if (index >= 0) {
        state.products[index] = product;
      } else {
        state.products.push(product);
      }

      if (!state.brands.includes(product.brand)) state.brands.push(product.brand);
      if (!state.categories.includes(product.categoryName)) state.categories.push(product.categoryName);

      writeStore();
      return deepClone(product);
    },

    async deleteProduct(id) {
      ensureState({});

      const productId = toNumber(id, 0);
      state.products = state.products.filter((item) => item.id !== productId);
      state.reviews = state.reviews.filter((item) => item.productId !== productId);
      writeStore();
      return { success: true };
    },

    async listOrders({ search = '', status = 'all', page = 1, pageSize = DEFAULT_PAGE_SIZE } = {}) {
      ensureState({});

      let filtered = state.orders.slice();
      if (status !== 'all') {
        filtered = filtered.filter((item) => item.status === status);
      }
      if (search) {
        filtered = filtered.filter((item) => (
          containsText(item.customerName, search)
          || containsText(item.customerEmail, search)
          || containsText(item.orderId, search)
        ));
      }

      filtered.sort((a, b) => new Date(b.orderDate).getTime() - new Date(a.orderDate).getTime());
      return paginate(filtered, page, pageSize);
    },

    async updateOrderStatus(orderId, status) {
      ensureState({});

      if (!ORDER_STATUSES.includes(status)) {
        return { success: false, message: 'Trạng thái không hợp lệ.' };
      }

      const id = toNumber(orderId, 0);
      const order = state.orders.find((item) => item.orderId === id);
      if (!order) return { success: false, message: 'Không tìm thấy đơn hàng.' };

      order.status = status;
      if (status === 'Shipping' && !order.trackingNumber) {
        order.trackingNumber = `VN${Math.floor(100000 + Math.random() * 900000)}`;
      }
      writeStore();
      return { success: true, order };
    },

    async listUsers({ search = '', page = 1, pageSize = DEFAULT_PAGE_SIZE } = {}) {
      ensureState({});

      let filtered = state.users.slice();
      if (search) {
        filtered = filtered.filter((item) => (
          containsText(item.fullName, search)
          || containsText(item.email, search)
          || containsText(item.phoneNumber, search)
          || containsText(item.role, search)
        ));
      }

      filtered.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
      return paginate(filtered, page, pageSize);
    },

    async listBanners({ search = '', page = 1, pageSize = DEFAULT_PAGE_SIZE } = {}) {
      ensureState({});

      let filtered = state.banners.slice();
      if (search) {
        filtered = filtered.filter((item) => (
          containsText(item.title, search)
          || containsText(item.linkUrl, search)
        ));
      }

      filtered.sort((a, b) => toNumber(a.displayOrder, 0) - toNumber(b.displayOrder, 0));
      return paginate(filtered, page, pageSize);
    },

    async upsertBanner(payload) {
      ensureState({});

      const id = toNumber(payload.id, 0);
      const banner = {
        id: id || (Math.max(0, ...state.banners.map((item) => toNumber(item.id, 0))) + 1),
        title: asString(payload.title, 'Banner mới'),
        imageUrl: asString(payload.imageUrl, 'https://placehold.co/600x240/f2f2f2/666?text=Banner'),
        linkUrl: asString(payload.linkUrl, 'products.html'),
        displayOrder: Math.max(0, toNumber(payload.displayOrder, 0)),
        isActive: Boolean(payload.isActive),
      };

      const index = state.banners.findIndex((item) => item.id === banner.id);
      if (index >= 0) {
        state.banners[index] = banner;
      } else {
        state.banners.push(banner);
      }

      writeStore();
      return deepClone(banner);
    },

    async deleteBanner(id) {
      ensureState({});
      const bannerId = toNumber(id, 0);
      state.banners = state.banners.filter((item) => item.id !== bannerId);
      writeStore();
      return { success: true };
    },

    async listCoupons({ search = '', page = 1, pageSize = DEFAULT_PAGE_SIZE } = {}) {
      ensureState({});

      let filtered = state.coupons.slice();
      if (search) {
        filtered = filtered.filter((item) => (
          containsText(item.code, search)
          || containsText(item.description, search)
        ));
      }

      filtered.sort((a, b) => toNumber(b.id, 0) - toNumber(a.id, 0));
      return paginate(filtered, page, pageSize);
    },

    async upsertCoupon(payload) {
      ensureState({});

      const id = toNumber(payload.id, 0);
      const coupon = {
        id: id || (Math.max(0, ...state.coupons.map((item) => toNumber(item.id, 0))) + 1),
        code: asString(payload.code, `CODE${Date.now()}`),
        description: asString(payload.description, ''),
        type: payload.type === 'amount' ? 'amount' : 'percent',
        value: Math.max(0, toNumber(payload.value, 0)),
        startDate: asString(payload.startDate, ''),
        endDate: asString(payload.endDate, ''),
        isActive: Boolean(payload.isActive),
      };

      const index = state.coupons.findIndex((item) => item.id === coupon.id);
      if (index >= 0) {
        state.coupons[index] = coupon;
      } else {
        state.coupons.push(coupon);
      }

      writeStore();
      return deepClone(coupon);
    },

    async deleteCoupon(id) {
      ensureState({});
      const couponId = toNumber(id, 0);
      state.coupons = state.coupons.filter((item) => item.id !== couponId);
      writeStore();
      return { success: true };
    },

    async listReviews({ search = '', status = 'all', page = 1, pageSize = DEFAULT_PAGE_SIZE } = {}) {
      ensureState({});

      let filtered = state.reviews.slice();
      if (status === 'pending') {
        filtered = filtered.filter((item) => !item.isApproved);
      }
      if (search) {
        filtered = filtered.filter((item) => (
          containsText(item.userName, search)
          || containsText(item.productName, search)
          || containsText(item.comment, search)
        ));
      }

      filtered.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
      return paginate(filtered, page, pageSize);
    },

    async approveReview(id) {
      ensureState({});
      const reviewId = toNumber(id, 0);
      const review = state.reviews.find((item) => item.reviewId === reviewId);
      if (!review) return { success: false };
      review.isApproved = true;
      writeStore();
      return { success: true };
    },

    async deleteReview(id) {
      ensureState({});
      const reviewId = toNumber(id, 0);
      state.reviews = state.reviews.filter((item) => item.reviewId !== reviewId);
      writeStore();
      return { success: true };
    },
  };

  function createApiAdapter(fallbackStore) {
    async function tryFetch(path, options = {}) {
      const base = window.PHONESTORE_API_BASE || (typeof window.getApiBase === 'function' ? window.getApiBase() : '') || '';
      if (!base) return null;

      try {
        const headers = {
          ...(window.Auth && typeof window.Auth.getAuthHeaders === 'function' ? window.Auth.getAuthHeaders() : {}),
          ...(options.headers || {}),
        };

        const response = await fetch(`${base}${path}`, {
          ...options,
          headers,
        });

        if (!response.ok) return null;
        return await response.json();
      } catch {
        return null;
      }
    }

    return {
      mode: 'api',

      async init(context = {}) {
        return fallbackStore.init(context);
      },

      async reset(context = {}) {
        return fallbackStore.reset(context);
      },

      getState() {
        return fallbackStore.getState();
      },

      async getStats() {
        const remote = await tryFetch('/admin/stats');
        if (remote && typeof remote.totalProducts !== 'undefined') {
          return remote;
        }
        return fallbackStore.getStats();
      },

      async getDashboardData() {
        return fallbackStore.getDashboardData();
      },

      async listProducts(params = {}) {
        return fallbackStore.listProducts(params);
      },

      async upsertProduct(payload) {
        return fallbackStore.upsertProduct(payload);
      },

      async deleteProduct(id) {
        return fallbackStore.deleteProduct(id);
      },

      async listOrders(params = {}) {
        const query = new URLSearchParams();
        if (params.status && params.status !== 'all') query.set('status', params.status);
        query.set('page', String(params.page || 1));
        query.set('pageSize', String(params.pageSize || DEFAULT_PAGE_SIZE));

        const remote = await tryFetch(`/admin/orders?${query.toString()}`);
        if (remote && Array.isArray(remote.orders)) {
          const mapped = remote.orders.map((item) => ({
            orderId: item.orderId,
            customerName: item.customerName || 'Khách',
            customerEmail: item.customerEmail || '',
            orderDate: item.orderDate,
            totalAmount: item.totalAmount,
            paymentMethod: item.paymentMethod,
            status: item.status,
            itemCount: item.itemCount,
            shippingAddress: item.shippingAddress,
            trackingNumber: item.trackingNumber,
          }));

          return {
            items: mapped,
            total: toNumber(remote.total, mapped.length),
            page: toNumber(remote.page, params.page || 1),
            pageSize: toNumber(remote.pageSize, params.pageSize || DEFAULT_PAGE_SIZE),
            totalPages: Math.max(1, Math.ceil(toNumber(remote.total, mapped.length) / toNumber(remote.pageSize, params.pageSize || DEFAULT_PAGE_SIZE))),
          };
        }

        return fallbackStore.listOrders(params);
      },

      async updateOrderStatus(orderId, status) {
        const remote = await tryFetch(`/admin/orders/${orderId}/status`, {
          method: 'PATCH',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ status }),
        });

        if (remote && remote.success) {
          await fallbackStore.updateOrderStatus(orderId, status);
          return { success: true, remote: true };
        }

        return fallbackStore.updateOrderStatus(orderId, status);
      },

      async listUsers(params = {}) {
        return fallbackStore.listUsers(params);
      },

      async listBanners(params = {}) {
        return fallbackStore.listBanners(params);
      },

      async upsertBanner(payload) {
        return fallbackStore.upsertBanner(payload);
      },

      async deleteBanner(id) {
        return fallbackStore.deleteBanner(id);
      },

      async listCoupons(params = {}) {
        return fallbackStore.listCoupons(params);
      },

      async upsertCoupon(payload) {
        return fallbackStore.upsertCoupon(payload);
      },

      async deleteCoupon(id) {
        return fallbackStore.deleteCoupon(id);
      },

      async listReviews(params = {}) {
        return fallbackStore.listReviews(params);
      },

      async approveReview(id) {
        return fallbackStore.approveReview(id);
      },

      async deleteReview(id) {
        return fallbackStore.deleteReview(id);
      },
    };
  }

  const useMock = window.ADMIN_USE_MOCK !== false;
  window.ADMIN_USE_MOCK = useMock;

  window.AdminStore = mockStore;
  window.AdminDataAccess = useMock ? mockStore : createApiAdapter(mockStore);
  window.ADMIN_ORDER_STATUSES = ORDER_STATUSES.slice();
})();
