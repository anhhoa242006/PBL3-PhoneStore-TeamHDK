// =====================
// MAIN.JS - Shared UI Logic
// =====================

// ---- Hero Carousel ----
function initCarousel() {
  const slides = document.querySelectorAll('.hero-slide');
  const dots = document.querySelectorAll('.carousel-dot');
  if (!slides.length) return;

  let current = 0;
  let timer;

  function goTo(n) {
    slides[current].classList.remove('active');
    dots[current]?.classList.remove('active');
    current = (n + slides.length) % slides.length;
    slides[current].classList.add('active');
    dots[current]?.classList.add('active');
  }

  function startAuto() {
    timer = setInterval(() => goTo(current + 1), 4500);
  }

  function stopAuto() { clearInterval(timer); }

  goTo(0);
  startAuto();

  document.querySelectorAll('.carousel-dot').forEach((dot, i) => {
    dot.addEventListener('click', () => { stopAuto(); goTo(i); startAuto(); });
  });

  document.querySelector('.carousel-prev')?.addEventListener('click', () => {
    stopAuto(); goTo(current - 1); startAuto();
  });

  document.querySelector('.carousel-next')?.addEventListener('click', () => {
    stopAuto(); goTo(current + 1); startAuto();
  });
}

// ---- Search Bar ----
function initSearch() {
  const input = document.getElementById('search-input');
  const dropdown = document.getElementById('search-dropdown');
  const form = document.getElementById('search-form');
  const recentSection = document.getElementById('recent-searches-section');
  const recentList = document.getElementById('recent-searches-list');
  const recommendedList = document.getElementById('search-recommended-list');
  
  if (!input || !dropdown) return;

  function loadRecommendations() {
    // 1. Fetch Recent Searches
    fetch('/Product/GetRecentSearches')
      .then(r => r.json())
      .then(data => {
        if (data && data.length > 0) {
          recentList.innerHTML = data.map(s => `
            <a href="/Product/Search?q=${encodeURIComponent(s)}" style="background:#f0f0f0; padding:4px 12px; border-radius:16px; font-size:12px; text-decoration:none; color:#333;">${s}</a>
          `).join('');
          recentSection.style.display = 'block';
        } else {
          recentSection.style.display = 'none';
        }
      });

    // 2. Fetch Personalized Recommendations
    fetch('/Product/GetPersonalizedRecommendations?take=5')
      .then(r => r.json())
      .then(data => {
        if (data && data.length > 0) {
          recommendedList.innerHTML = data.map(p => `
            <div class="search-result-item" onclick="location.href='/product/${p.slug || p.id}${p.versionId ? '?versionId=' + p.versionId : ''}'">
              <img src="${p.imageUrl}" alt="${p.name}" class="search-result-img" loading="lazy">
              <div>
                <div class="search-result-name">${p.name}</div>
                <div class="search-result-price">${new Intl.NumberFormat('vi-VN').format(p.price)}₫</div>
              </div>
            </div>
          `).join('');
        }
      });
  }

  input.addEventListener('focus', () => {
    if (input.value.trim().length === 0) {
      loadRecommendations();
      dropdown.classList.add('active');
      dropdown.style.display = 'block';
    }
  });

  let debounceTimer;
  input.addEventListener('input', () => {
    clearTimeout(debounceTimer);
    const kw = input.value.trim();
    
    if (kw.length === 0) {
      loadRecommendations();
      dropdown.classList.add('active');
      dropdown.style.display = 'block';
      return;
    }

    debounceTimer = setTimeout(() => {
      if (kw.length < 2) return;

      fetch(`/Product/GetProducts?q=${encodeURIComponent(kw)}`)
        .then(r => r.json())
        .then(data => {
            const results = data.filter(p => p.name.toLowerCase().includes(kw.toLowerCase())).slice(0, 6);
            if (!results.length) {
                dropdown.innerHTML = '<div style="padding:15px; text-align:center; color:#999;">Không tìm thấy sản phẩm nào</div>';
            } else {
                dropdown.innerHTML = results.map(p => `
                    <div class="search-result-item" onclick="location.href='/product/${p.slug || p.id}${p.versionId ? '?versionId=' + p.versionId : ''}'">
                      <img src="${p.imageUrl}" alt="${p.name}" class="search-result-img" loading="lazy">
                      <div>
                        <div class="search-result-name">${p.name}</div>
                        <div class="search-result-price">${new Intl.NumberFormat('vi-VN').format(p.price)}₫</div>
                      </div>
                    </div>
                `).join('');
            }
            dropdown.classList.add('active');
            dropdown.style.display = 'block';
        });
    }, 300);
  });

  // Close dropdown on click outside
  document.addEventListener('click', (e) => {
    if (!input.closest('.search-bar').contains(e.target)) {
      dropdown.classList.remove('active');
      dropdown.style.display = 'none';
    }
  });
}

// ---- Mobile Menu ----
function initMobileMenu() {
  const btn = document.getElementById('mobile-menu-btn');
  const overlay = document.getElementById('mobile-menu-overlay');
  const menu = document.getElementById('mobile-menu');
  const closeBtn = document.getElementById('mobile-menu-close');
  if (!btn) return;

  function openMenu() {
    overlay.classList.add('open');
    menu.classList.add('open');
    document.body.style.overflow = 'hidden';
  }

  function closeMenu() {
    overlay.classList.remove('open');
    menu.classList.remove('open');
    document.body.style.overflow = '';
  }

  btn.addEventListener('click', openMenu);
  overlay?.addEventListener('click', closeMenu);
  closeBtn?.addEventListener('click', closeMenu);
}

// ---- Tabs ----
function initTabs() {
  document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      const tabGroup = btn.closest('.tabs');
      const target = btn.dataset.tab;

      tabGroup.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
      tabGroup.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));

      btn.classList.add('active');
      tabGroup.querySelector(`#${target}`)?.classList.add('active');
    });
  });
}

// ---- Render helpers ----
function renderStars(rating) {
  const full = Math.floor(rating);
  const half = rating % 1 >= 0.5;
  const empty = 5 - full - (half ? 1 : 0);
  return '★'.repeat(full) + (half ? '½' : '') + '☆'.repeat(empty);
}

function renderProductCard(product) {
  const badge = product.isHot
    ? `<span class="product-badge badge-hot">HOT</span>`
    : product.discount > 0
    ? `<span class="product-badge badge-sale">-${product.discount}%</span>`
    : '';

  return `
    <div class="product-card" onclick="location.href='product-detail.html?id=${product.id}'">
      <div class="product-card-img-wrapper">
        ${badge}
        <img src="${product.image}" alt="${product.name}" class="product-card-img" loading="lazy">
        <button class="product-wishlist-btn" onclick="event.stopPropagation(); toggleWishlist(${product.id}, this)" title="Yêu thích">♡</button>
      </div>
      <div class="product-card-body">
        <div class="product-brand">${product.brand}</div>
        <div class="product-name">${product.name}</div>
        <div class="product-rating">
          <span class="stars">${renderStars(product.rating)}</span>
          <span class="review-count">(${product.reviewCount.toLocaleString('vi-VN')})</span>
        </div>
        <div class="product-pricing">
          <span class="product-price">${formatPrice(product.price)}</span>
          ${product.originalPrice > product.price
            ? `<span class="product-original-price">${formatPrice(product.originalPrice)}</span>
               <span class="product-discount">-${product.discount}%</span>`
            : ''
          }
        </div>
      </div>
      <div class="product-card-footer">
        <button class="add-to-cart-btn" onclick="event.stopPropagation(); quickAddToCart(${product.id})">
          🛒 Thêm vào giỏ
        </button>
      </div>
    </div>
  `;
}

// Quick add to cart (uses first variant)
function quickAddToCart(productId) {
  const product = getProductById(productId);
  if (!product) return;
  const variant = product.variants[0] || { color: '', storage: '', price: product.price };
  Cart.addItem(product, variant, 1);
}

// Wishlist toggle (Server-side)
function toggleWishlist(event, productId, versionId, btn) {
    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }
    
    $.post('/Wishlist/Toggle', { productId: productId, versionId: versionId == 0 ? null : versionId }, function(res) {
        if(res.success) {
            if(res.isAdded) {
                btn.classList.add('active');
                // If it's the heart icon version
                const svg = btn.querySelector('svg');
                if(svg) svg.setAttribute('fill', '#d70018');
            } else {
                btn.classList.remove('active');
                const svg = btn.querySelector('svg');
                if(svg) svg.setAttribute('fill', 'currentColor');
            }
            
            updateWishlistBadge(res.count);
            
            // Show modern toast
            if(typeof showSuccessToast === 'function') {
                showSuccessToast(res.message);
            } else {
                alert(res.message);
            }
        } else {
            // If not logged in, show info toast then redirect
            if(typeof showSuccessToast === 'function') {
                showSuccessToast(res.message);
                setTimeout(() => {
                    window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
                }, 1500);
            } else {
                window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
            }
        }
    }).fail(function() {
        window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
    });
}

// Function specifically for Home/Search results if needed
function toggleHomeWishlist(event, productId, versionId, btn) {
    toggleWishlist(event, productId, versionId, btn);
}

function updateWishlistBadge(count) {
    const badge = document.getElementById('wishlist-badge');
    if(badge) {
        badge.textContent = count;
        badge.style.display = count > 0 ? 'block' : 'none';
    }
}

// Active nav item
function setActiveNav() {
  const path = window.location.pathname;
  document.querySelectorAll('.nav-item').forEach(item => {
    const href = item.getAttribute('href');
    if (href && path.includes(href.replace('.html', ''))) {
      item.classList.add('active');
    }
  });
}

function renderNavigation() {
  if (!Array.isArray(categories) || !categories.length) return;

  const navContainer = document.querySelector('.header-nav .container');
  if (navContainer) {
    const categoryLinks = categories.map(c => `
      <a href="products.html?cat=${c.id}" class="nav-item">${c.icon} ${c.name}</a>
    `).join('');
    navContainer.innerHTML = `
      ${categoryLinks}
      <a href="orders.html" class="nav-item">📦 Đơn hàng</a>
      <a href="products.html" class="nav-item" style="margin-left:auto;background:rgba(255,255,255,.15)">🔥 Khuyến mãi</a>
    `;
  }

  const mobileMenu = document.getElementById('mobile-menu');
  if (mobileMenu) {
    mobileMenu.querySelectorAll('.mobile-nav-item').forEach(item => item.remove());
    const links = [
      { href: 'index.html', label: '🏠 Trang chủ' },
      ...categories.map(c => ({ href: `products.html?cat=${c.id}`, label: `${c.icon} ${c.name}` })),
      { href: 'cart.html', label: '🛒 Giỏ hàng' },
      { href: 'orders.html', label: '📦 Đơn hàng' },
      { href: 'auth.html', label: '👤 Tài khoản' },
    ];

    links.forEach(link => {
      const a = document.createElement('a');
      a.href = link.href;
      a.className = 'mobile-nav-item';
      a.textContent = link.label;
      mobileMenu.appendChild(a);
    });
  }
}

// Init on DOM ready
document.addEventListener('DOMContentLoaded', async () => {
  if (typeof loadCatalog === 'function') {
    await loadCatalog();
  }
  renderNavigation();
  initCarousel();
  initSearch();
  initMobileMenu();
  initTabs();
  setActiveNav();
  
  // Load wishlist count
  fetch('/Wishlist/GetCount')
    .then(r => r.json())
    .then(count => updateWishlistBadge(count));
});
