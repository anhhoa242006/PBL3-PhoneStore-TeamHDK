// =====================
// PRODUCT, CATEGORY, BANNER DATA
// =====================

const DEFAULT_API_BASE = 'http://localhost:5000/api';
const PHONESTORE_API_BASE = window.PHONESTORE_API_BASE
  || localStorage.getItem('phonestore_api_base')
  || DEFAULT_API_BASE;

window.PHONESTORE_API_BASE = PHONESTORE_API_BASE;
window.getApiBase = () => PHONESTORE_API_BASE;

let catalogPromise = null;
let categories = [];
let brands = [];
let products = [];
let banners = [];

const DEMO_CATEGORIES = [
  { id: 'dien-thoai', name: 'Điện thoại', count: 10 },
  { id: 'may-tinh-bang', name: 'Máy tính bảng', count: 8 },
  { id: 'phu-kien', name: 'Phụ kiện', count: 14 },
  { id: 'laptop', name: 'Laptop', count: 8 },
];

const DEMO_BRANDS = ['Apple', 'Samsung', 'Xiaomi', 'OPPO', 'Sony', 'Infinix', 'Realme', 'Huawei', 'iPad', 'Honor', 'Teclast', 'MacBook', 'ASUS', 'Lenovo', 'DELL', 'HP', 'ACER', 'LG', 'MSI'];

const DEMO_PRODUCTS = [
  {
    id: 101,
    name: 'iPhone 16 Pro Max',
    brand: 'Apple',
    category: 'dien-thoai',
    categoryName: 'Điện thoại',
    price: 34990000,
    originalPrice: 37990000,
    rating: 4.9,
    reviewCount: 128,
    description: 'Mẫu flagship với camera Pro, hiệu năng mạnh và màn hình siêu sáng.',
    specs: { 'Màn hình': '6.9 inch', 'Chip': 'A18 Pro', 'Pin': 'Dùng cả ngày' },
    variants: [
      { color: 'Titan tự nhiên', storage: '256GB', price: 34990000, originalPrice: 37990000, stock: 12, image: 'img/Dien-thoai/iphone-16promax/iphone-16-pro-max-256gb.png' },
      { color: 'Titan tự nhiên', storage: '512GB', price: 38990000, originalPrice: 41990000, stock: 9, image: 'https://placehold.co/800x800/8f8879/ffffff?text=iPhone+16+Pro+Max+Natural+Titanium' },
      { color: 'Titan tự nhiên', storage: '1TB', price: 43990000, originalPrice: 46990000, stock: 6, image: 'https://placehold.co/800x800/8f8879/ffffff?text=iPhone+16+Pro+Max+Natural+Titanium' },
      { color: 'Titan đen', storage: '256GB', price: 35190000, originalPrice: 38190000, stock: 10, image: 'img/Dien-thoai/iphone-16promax/iphone-16-pro-max-main.webp' },
      { color: 'Titan đen', storage: '512GB', price: 39990000, originalPrice: 42990000, stock: 8, image: 'img/Dien-thoai/iphone-16promax/iphone-16-pro-max-main.webp' },
      { color: 'Titan đen', storage: '1TB', price: 44990000, originalPrice: 47990000, stock: 5, image: 'img/Dien-thoai/iphone-16promax/iphone-16-pro-max-main.webp' },
      { color: 'Titan trắng', storage: '256GB', price: 35490000, originalPrice: 38490000, stock: 11, image: 'https://placehold.co/800x800/e5e7eb/111827?text=iPhone+16+Pro+Max+White+Titanium' },
      { color: 'Titan trắng', storage: '512GB', price: 40490000, originalPrice: 43490000, stock: 7, image: 'https://placehold.co/800x800/e5e7eb/111827?text=iPhone+16+Pro+Max+White+Titanium' },
      { color: 'Titan trắng', storage: '1TB', price: 45490000, originalPrice: 48490000, stock: 4, image: 'https://placehold.co/800x800/e5e7eb/111827?text=iPhone+16+Pro+Max+White+Titanium' },
      { color: 'Titan sa mạc', storage: '256GB', price: 35690000, originalPrice: 38690000, stock: 9, image: 'https://placehold.co/800x800/b08968/ffffff?text=iPhone+16+Pro+Max+Desert+Titanium' },
      { color: 'Titan sa mạc', storage: '512GB', price: 40690000, originalPrice: 43690000, stock: 6, image: 'https://placehold.co/800x800/b08968/ffffff?text=iPhone+16+Pro+Max+Desert+Titanium' },
      { color: 'Titan sa mạc', storage: '1TB', price: 45690000, originalPrice: 48690000, stock: 4, image: 'https://placehold.co/800x800/b08968/ffffff?text=iPhone+16+Pro+Max+Desert+Titanium' },
    ],
    images: [
      'img/Dien-thoai/iphone-16promax/iphone-16-pro-max-main.webp',
      'img/Dien-thoai/iphone-16promax/iphone-16-pro-max-gallery.avif',
      'img/Dien-thoai/iphone-16promax/iphone-16-pro-max-256gb.png',
      'https://placehold.co/800x800/e5e7eb/111827?text=iPhone+16+Pro+Max+White+Titanium',
      'https://placehold.co/800x800/b08968/ffffff?text=iPhone+16+Pro+Max+Desert+Titanium',
      'https://placehold.co/800x800/8f8879/ffffff?text=iPhone+16+Pro+Max+Natural+Titanium',
    ],
    image: 'img/Dien-thoai/iphone-16promax/iphone-16-pro-max-main.webp',
    isFeatured: true,
    isOnSale: true,
    isHot: true,
  },
  {
    id: 102,
    name: 'Samsung Galaxy S25 Ultra',
    brand: 'Samsung',
    category: 'dien-thoai',
    categoryName: 'Điện thoại',
    price: 31990000,
    originalPrice: 34990000,
    rating: 4.8,
    reviewCount: 96,
    description: 'Màn hình lớn, bút S Pen và camera zoom ấn tượng cho demo cao cấp.',
    specs: { 'Màn hình': '6.8 inch', 'Chip': 'Snapdragon 8 Gen 4', 'Pin': '5000mAh' },
    variants: [
      { color: 'Đen', storage: '256GB', price: 31990000, originalPrice: 34990000, stock: 10, image: 'https://placehold.co/800x800/0f172a/ffffff?text=Galaxy+S25+Ultra+Black' },
      { color: 'Đen', storage: '512GB', price: 35990000, originalPrice: 38990000, stock: 7, image: 'https://placehold.co/800x800/0f172a/ffffff?text=Galaxy+S25+Ultra+Black' },
      { color: 'Đen', storage: '1TB', price: 39990000, originalPrice: 42990000, stock: 4, image: 'https://placehold.co/800x800/0f172a/ffffff?text=Galaxy+S25+Ultra+Black' },
      { color: 'Titan xám', storage: '256GB', price: 32290000, originalPrice: 35290000, stock: 8, image: 'https://placehold.co/800x800/6b7280/ffffff?text=Galaxy+S25+Ultra+Titan+Gray' },
      { color: 'Titan xám', storage: '512GB', price: 36290000, originalPrice: 39290000, stock: 6, image: 'https://placehold.co/800x800/6b7280/ffffff?text=Galaxy+S25+Ultra+Titan+Gray' },
      { color: 'Titan xám', storage: '1TB', price: 40290000, originalPrice: 43290000, stock: 3, image: 'https://placehold.co/800x800/6b7280/ffffff?text=Galaxy+S25+Ultra+Titan+Gray' },
      { color: 'Bạc', storage: '256GB', price: 32290000, originalPrice: 35290000, stock: 9, image: 'https://placehold.co/800x800/cbd5e1/0f172a?text=Galaxy+S25+Ultra+Silver' },
      { color: 'Bạc', storage: '512GB', price: 36290000, originalPrice: 39290000, stock: 5, image: 'https://placehold.co/800x800/cbd5e1/0f172a?text=Galaxy+S25+Ultra+Silver' },
      { color: 'Bạc', storage: '1TB', price: 40290000, originalPrice: 43290000, stock: 4, image: 'https://placehold.co/800x800/cbd5e1/0f172a?text=Galaxy+S25+Ultra+Silver' },
      { color: 'Xanh navy', storage: '256GB', price: 32490000, originalPrice: 35490000, stock: 8, image: 'https://placehold.co/800x800/1e3a8a/ffffff?text=Galaxy+S25+Ultra+Navy' },
      { color: 'Xanh navy', storage: '512GB', price: 36490000, originalPrice: 39490000, stock: 6, image: 'https://placehold.co/800x800/1e3a8a/ffffff?text=Galaxy+S25+Ultra+Navy' },
      { color: 'Xanh navy', storage: '1TB', price: 40490000, originalPrice: 43490000, stock: 3, image: 'https://placehold.co/800x800/1e3a8a/ffffff?text=Galaxy+S25+Ultra+Navy' },
    ],
    images: [
      'https://placehold.co/800x800/0f172a/ffffff?text=Galaxy+S25+Ultra+Black',
      'https://placehold.co/800x800/cbd5e1/0f172a?text=Galaxy+S25+Ultra+Silver',
      'https://placehold.co/800x800/1e3a8a/ffffff?text=Galaxy+S25+Ultra+Navy',
      'https://placehold.co/800x800/6b7280/ffffff?text=Galaxy+S25+Ultra+Titan+Gray',
      'https://placehold.co/800x800/1f2937/ffffff?text=S+Pen',
    ],
    image: 'https://placehold.co/800x800/0f172a/ffffff?text=Galaxy+S25+Ultra+Black',
    isFeatured: true,
    isOnSale: true,
    isHot: true,
  },
  {
    id: 103,
    name: 'Xiaomi 15',
    brand: 'Xiaomi',
    category: 'dien-thoai',
    categoryName: 'Điện thoại',
    price: 18990000,
    originalPrice: 20990000,
    rating: 4.7,
    reviewCount: 74,
    description: 'Thiết kế gọn, hiệu năng ổn định và camera hợp để demo phân khúc cận cao cấp.',
    specs: { 'Màn hình': '6.36 inch', 'Chip': 'Snapdragon 8 Gen 4', 'Pin': '4800mAh' },
    variants: [
      { color: 'Trắng', storage: '256GB', price: 18990000, originalPrice: 20990000, stock: 14, image: 'https://placehold.co/800x800/082f49/ffffff?text=Xiaomi+15' },
      { color: 'Xanh lá', storage: '512GB', price: 21990000, originalPrice: 23990000, stock: 6, image: 'https://placehold.co/800x800/082f49/ffffff?text=Xiaomi+15' },
    ],
    images: [
      'https://placehold.co/800x800/082f49/ffffff?text=Xiaomi+15',
      'https://placehold.co/800x800/0ea5e9/ffffff?text=Leica+Camera',
    ],
    image: 'https://placehold.co/800x800/082f49/ffffff?text=Xiaomi+15',
    isFeatured: true,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 104,
    name: 'OPPO Reno13 Pro',
    brand: 'OPPO',
    category: 'dien-thoai',
    categoryName: 'Điện thoại',
    price: 14990000,
    originalPrice: 16990000,
    rating: 4.6,
    reviewCount: 61,
    description: 'Mẫu máy tập trung vào camera chân dung và màu sắc nổi bật, hợp demo tầm trung.',
    specs: { 'Màn hình': '6.7 inch', 'Chip': 'Dimensity 8300', 'Pin': '5000mAh' },
    variants: [
      { color: 'Hồng', storage: '256GB', price: 14990000, originalPrice: 16990000, stock: 15, image: 'https://placehold.co/800x800/831843/ffffff?text=OPPO+Reno13+Pro' },
      { color: 'Xanh', storage: '512GB', price: 17990000, originalPrice: 19990000, stock: 5, image: 'https://placehold.co/800x800/831843/ffffff?text=OPPO+Reno13+Pro' },
    ],
    images: [
      'https://placehold.co/800x800/831843/ffffff?text=OPPO+Reno13+Pro',
      'https://placehold.co/800x800/be185d/ffffff?text=Portrait+Camera',
    ],
    image: 'https://placehold.co/800x800/831843/ffffff?text=OPPO+Reno13+Pro',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 201,
    name: 'iPad Air M2',
    brand: 'Apple',
    category: 'may-tinh-bang',
    categoryName: 'Máy tính bảng',
    price: 15990000,
    originalPrice: 17990000,
    rating: 4.8,
    reviewCount: 52,
    description: 'Máy tính bảng gọn nhẹ, phù hợp trình diễn học tập và sáng tạo nội dung.',
    specs: { 'Màn hình': '11 inch', 'Chip': 'Apple M2', 'Pin': '10 giờ' },
    variants: [
      { color: 'Xanh', storage: '128GB', price: 15990000, originalPrice: 17990000, stock: 11, image: 'https://placehold.co/800x800/1e3a8a/ffffff?text=iPad+Air+M2' },
      { color: 'Xám', storage: '256GB', price: 18990000, originalPrice: 20990000, stock: 5, image: 'https://placehold.co/800x800/1e3a8a/ffffff?text=iPad+Air+M2' },
    ],
    images: [
      'https://placehold.co/800x800/1e3a8a/ffffff?text=iPad+Air+M2',
      'https://placehold.co/800x800/312e81/ffffff?text=Apple+Pencil',
    ],
    image: 'https://placehold.co/800x800/1e3a8a/ffffff?text=iPad+Air+M2',
    isFeatured: true,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 202,
    name: 'Samsung Galaxy Tab S10',
    brand: 'Samsung',
    category: 'may-tinh-bang',
    categoryName: 'Máy tính bảng',
    price: 17990000,
    originalPrice: 19990000,
    rating: 4.7,
    reviewCount: 45,
    description: 'Màn hình lớn, loa tốt, phù hợp demo giải trí, học tập và làm việc.',
    specs: { 'Màn hình': '12.4 inch', 'Chip': 'Snapdragon 8 Gen 3', 'Pin': '10090mAh' },
    variants: [
      { color: 'Đen', storage: '256GB', price: 17990000, originalPrice: 19990000, stock: 9, image: 'https://placehold.co/800x800/111827/ffffff?text=Galaxy+Tab+S10' },
      { color: 'Bạc', storage: '512GB', price: 20990000, originalPrice: 22990000, stock: 4, image: 'https://placehold.co/800x800/111827/ffffff?text=Galaxy+Tab+S10' },
    ],
    images: [
      'https://placehold.co/800x800/111827/ffffff?text=Galaxy+Tab+S10',
      'https://placehold.co/800x800/374151/ffffff?text=S+Pen',
    ],
    image: 'https://placehold.co/800x800/111827/ffffff?text=Galaxy+Tab+S10',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 301,
    name: 'AirPods Pro 2',
    brand: 'Apple',
    category: 'phu-kien',
    categoryName: 'Phụ kiện',
    price: 5490000,
    originalPrice: 5990000,
    rating: 4.9,
    reviewCount: 210,
    description: 'Tai nghe true wireless rất hợp để demo phụ kiện bán chạy.',
    specs: { 'Kết nối': 'Bluetooth 5.3', 'Pin': '30 giờ', 'Chống ồn': 'Có' },
    variants: [
      { color: 'Trắng', storage: '', price: 5490000, originalPrice: 5990000, stock: 20, image: 'https://placehold.co/800x800/0f172a/ffffff?text=AirPods+Pro+2' },
    ],
    images: [
      'https://placehold.co/800x800/0f172a/ffffff?text=AirPods+Pro+2',
      'https://placehold.co/800x800/111827/ffffff?text=Noise+Canceling',
    ],
    image: 'https://placehold.co/800x800/0f172a/ffffff?text=AirPods+Pro+2',
    isFeatured: true,
    isOnSale: true,
    isHot: true,
  },
  {
    id: 302,
    name: 'Sạc nhanh 20W Anker',
    brand: 'Anker',
    category: 'phu-kien',
    categoryName: 'Phụ kiện',
    price: 390000,
    originalPrice: 490000,
    rating: 4.8,
    reviewCount: 155,
    description: 'Món phụ kiện giá tốt, phù hợp demo combo mua kèm.',
    specs: { 'Công suất': '20W', 'Cổng': 'USB-C', 'Màu': 'Trắng' },
    variants: [
      { color: 'Trắng', storage: '', price: 390000, originalPrice: 490000, stock: 50, image: 'https://placehold.co/800x800/ffffff/0f172a?text=Anker+20W' },
    ],
    images: [
      'https://placehold.co/800x800/ffffff/0f172a?text=Anker+20W',
    ],
    image: 'https://placehold.co/800x800/ffffff/0f172a?text=Anker+20W',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 303,
    name: 'Tai nghe Sony WH-1000XM5',
    brand: 'Sony',
    category: 'phu-kien',
    categoryName: 'Phụ kiện',
    price: 8290000,
    originalPrice: 8990000,
    rating: 4.8,
    reviewCount: 88,
    description: 'Tai nghe over-ear cao cấp để tạo cảm giác shop phụ kiện đầy đủ.',
    specs: { 'Kết nối': 'Bluetooth', 'Pin': '30 giờ', 'Chống ồn': 'Thích ứng' },
    variants: [
      { color: 'Đen', storage: '', price: 8290000, originalPrice: 8990000, stock: 13, image: 'https://placehold.co/800x800/111827/ffffff?text=Sony+XM5' },
      { color: 'Bạc', storage: '', price: 8490000, originalPrice: 9190000, stock: 7, image: 'https://placehold.co/800x800/111827/ffffff?text=Sony+XM5' },
    ],
    images: [
      'https://placehold.co/800x800/111827/ffffff?text=Sony+XM5',
      'https://placehold.co/800x800/374151/ffffff?text=Noise+Canceling',
    ],
    image: 'https://placehold.co/800x800/111827/ffffff?text=Sony+XM5',
    isFeatured: false,
    isOnSale: true,
    isHot: true,
  },
  {
    id: 304,
    name: 'Bàn phím Logitech MX Keys',
    brand: 'Logitech',
    category: 'phu-kien',
    categoryName: 'Phụ kiện',
    price: 2790000,
    originalPrice: 3290000,
    rating: 4.7,
    reviewCount: 63,
    description: 'Phụ kiện văn phòng đẹp, giúp trang demo có nhiều sản phẩm đa dạng hơn.',
    specs: { 'Kết nối': 'Bluetooth', 'Pin': '10 ngày', 'Layout': 'Full size' },
    variants: [
      { color: 'Xám', storage: '', price: 2790000, originalPrice: 3290000, stock: 18, image: 'https://placehold.co/800x800/374151/ffffff?text=MX+Keys' },
    ],
    images: [
      'https://placehold.co/800x800/374151/ffffff?text=MX+Keys',
    ],
    image: 'https://placehold.co/800x800/374151/ffffff?text=MX+Keys',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 401,
    name: 'MacBook Air M3',
    brand: 'Apple',
    category: 'laptop',
    categoryName: 'Laptop',
    price: 28990000,
    originalPrice: 31990000,
    rating: 4.9,
    reviewCount: 77,
    description: 'Laptop mỏng nhẹ, pin tốt, phù hợp trang demo cao cấp cho sinh viên và dân văn phòng.',
    specs: { 'Màn hình': '13.6 inch', 'Chip': 'Apple M3', 'Pin': '18 giờ' },
    variants: [
      { color: 'Bạc', storage: '256GB SSD', price: 28990000, originalPrice: 31990000, stock: 9, image: 'https://placehold.co/800x800/0f172a/ffffff?text=MacBook+Air+M3' },
      { color: 'Xanh', storage: '512GB SSD', price: 32990000, originalPrice: 35990000, stock: 5, image: 'https://placehold.co/800x800/0f172a/ffffff?text=MacBook+Air+M3' },
    ],
    images: [
      'https://placehold.co/800x800/0f172a/ffffff?text=MacBook+Air+M3',
      'https://placehold.co/800x800/1e293b/ffffff?text=Apple+M3',
    ],
    image: 'https://placehold.co/800x800/0f172a/ffffff?text=MacBook+Air+M3',
    isFeatured: true,
    isOnSale: true,
    isHot: true,
  },
  {
    id: 402,
    name: 'Dell Inspiron 14',
    brand: 'Dell',
    category: 'laptop',
    categoryName: 'Laptop',
    price: 15990000,
    originalPrice: 17990000,
    rating: 4.6,
    reviewCount: 54,
    description: 'Mẫu laptop phổ thông, dễ demo cho học tập, làm việc và phân khúc giá tốt.',
    specs: { 'Màn hình': '14 inch', 'Chip': 'Intel Core i5', 'RAM': '16GB' },
    variants: [
      { color: 'Đen', storage: '512GB SSD', price: 15990000, originalPrice: 17990000, stock: 14, image: 'https://placehold.co/800x800/111827/ffffff?text=Dell+Inspiron+14' },
    ],
    images: [
      'https://placehold.co/800x800/111827/ffffff?text=Dell+Inspiron+14',
    ],
    image: 'https://placehold.co/800x800/111827/ffffff?text=Dell+Inspiron+14',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 403,
    name: 'ASUS Vivobook 15',
    brand: 'ASUS',
    category: 'laptop',
    categoryName: 'Laptop',
    price: 13990000,
    originalPrice: 15990000,
    rating: 4.5,
    reviewCount: 41,
    description: 'Laptop đa dụng cho sinh viên, có giá vừa phải để làm nổi bật khuyến mãi.',
    specs: { 'Màn hình': '15.6 inch', 'Chip': 'Intel Core i5', 'RAM': '8GB' },
    variants: [
      { color: 'Xanh', storage: '512GB SSD', price: 13990000, originalPrice: 15990000, stock: 10, image: 'https://placehold.co/800x800/1d4ed8/ffffff?text=ASUS+Vivobook+15' },
    ],
    images: [
      'https://placehold.co/800x800/1d4ed8/ffffff?text=ASUS+Vivobook+15',
    ],
    image: 'https://placehold.co/800x800/1d4ed8/ffffff?text=ASUS+Vivobook+15',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 404,
    name: 'HP Pavilion 14',
    brand: 'HP',
    category: 'laptop',
    categoryName: 'Laptop',
    price: 16990000,
    originalPrice: 18990000,
    rating: 4.6,
    reviewCount: 37,
    description: 'Thiết kế thanh lịch, phù hợp demo khu vực laptop doanh nghiệp và học tập.',
    specs: { 'Màn hình': '14 inch', 'Chip': 'Intel Core i7', 'RAM': '16GB' },
    variants: [
      { color: 'Bạc', storage: '512GB SSD', price: 16990000, originalPrice: 18990000, stock: 8, image: 'https://placehold.co/800x800/64748b/ffffff?text=HP+Pavilion+14' },
    ],
    images: [
      'https://placehold.co/800x800/64748b/ffffff?text=HP+Pavilion+14',
    ],
    image: 'https://placehold.co/800x800/64748b/ffffff?text=HP+Pavilion+14',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 405,
    name: 'Realme 14 Pro',
    brand: 'Realme',
    category: 'dien-thoai',
    categoryName: 'Điện thoại',
    price: 17990000,
    originalPrice: 19990000,
    rating: 4.7,
    reviewCount: 89,
    description: 'Thiết kế hiện đại, chip mạnh, camera chất lượng cao.',
    specs: { 'Màn hình': '6.7 inch', 'Chip': 'Snapdragon 8 Gen 3', 'Pin': '5500mAh' },
    variants: [
      { color: 'Xanh', storage: '256GB', price: 17990000, originalPrice: 19990000, stock: 10, image: 'https://placehold.co/800x800/1e40af/ffffff?text=Realme+14+Pro' },
      { color: 'Vàng', storage: '512GB', price: 20990000, originalPrice: 22990000, stock: 6, image: 'https://placehold.co/800x800/1e40af/ffffff?text=Realme+14+Pro' },
    ],
    images: [
      'https://placehold.co/800x800/1e40af/ffffff?text=Realme+14+Pro',
      'https://placehold.co/800x800/0f172a/ffffff?text=Camera+System',
    ],
    image: 'https://placehold.co/800x800/1e40af/ffffff?text=Realme+14+Pro',
    isFeatured: true,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 406,
    name: 'Vivo X100',
    brand: 'Vivo',
    category: 'dien-thoai',
    categoryName: 'Điện thoại',
    price: 19990000,
    originalPrice: 21990000,
    rating: 4.8,
    reviewCount: 112,
    description: 'Camera AI nâng cao, chip Snapdragon mạnh mẽ, pin dài.',
    specs: { 'Màn hình': '6.78 inch', 'Chip': 'Snapdragon 8 Gen 3 Leading', 'Pin': '6000mAh' },
    variants: [
      { color: 'Đen', storage: '256GB', price: 19990000, originalPrice: 21990000, stock: 9, image: 'https://placehold.co/800x800/111827/ffffff?text=Vivo+X100' },
      { color: 'Trắng', storage: '512GB', price: 22990000, originalPrice: 24990000, stock: 7, image: 'https://placehold.co/800x800/111827/ffffff?text=Vivo+X100' },
    ],
    images: [
      'https://placehold.co/800x800/111827/ffffff?text=Vivo+X100',
      'https://placehold.co/800x800/374151/ffffff?text=AI+Camera',
    ],
    image: 'https://placehold.co/800x800/111827/ffffff?text=Vivo+X100',
    isFeatured: true,
    isOnSale: true,
    isHot: true,
  },
  {
    id: 407,
    name: 'Google Pixel 9',
    brand: 'Google',
    category: 'dien-thoai',
    categoryName: 'Điện thoại',
    price: 22990000,
    originalPrice: 24990000,
    rating: 4.9,
    reviewCount: 156,
    description: 'Tùy chỉnh phần cứng-phần mềm hoàn hảo, xử lý hình ảnh AI tuyệt vời.',
    specs: { 'Màn hình': '6.3 inch', 'Chip': 'Tensor 4', 'Pin': '5100mAh' },
    variants: [
      { color: 'Obsidian', storage: '256GB', price: 22990000, originalPrice: 24990000, stock: 11, image: 'https://placehold.co/800x800/000000/ffffff?text=Pixel+9' },
      { color: 'Porcelain', storage: '512GB', price: 25990000, originalPrice: 27990000, stock: 8, image: 'https://placehold.co/800x800/000000/ffffff?text=Pixel+9' },
    ],
    images: [
      'https://placehold.co/800x800/000000/ffffff?text=Pixel+9',
      'https://placehold.co/800x800/1f2937/ffffff?text=Magic+Eraser',
    ],
    image: 'https://placehold.co/800x800/000000/ffffff?text=Pixel+9',
    isFeatured: true,
    isOnSale: false,
    isHot: true,
  },
  {
    id: 408,
    name: 'Huawei Mate 70',
    brand: 'Huawei',
    category: 'dien-thoai',
    categoryName: 'Điện thoại',
    price: 23990000,
    originalPrice: 25990000,
    rating: 4.7,
    reviewCount: 78,
    description: 'Thiết kế sang trọng, công nghệ HarmonyOS, hiệu năng tốt.',
    specs: { 'Màn hình': '6.7 inch', 'Chip': 'Kirin 9015', 'Pin': '5610mAh' },
    variants: [
      { color: 'Bạc', storage: '256GB', price: 23990000, originalPrice: 25990000, stock: 7, image: 'https://placehold.co/800x800/94a3b8/ffffff?text=Mate+70' },
      { color: 'Đen', storage: '512GB', price: 26990000, originalPrice: 28990000, stock: 5, image: 'https://placehold.co/800x800/94a3b8/ffffff?text=Mate+70' },
    ],
    images: [
      'https://placehold.co/800x800/94a3b8/ffffff?text=Mate+70',
      'https://placehold.co/800x800/cbd5e1/ffffff?text=HarmonyOS',
    ],
    image: 'https://placehold.co/800x800/94a3b8/ffffff?text=Mate+70',
    isFeatured: false,
    isOnSale: true,
    isHot: true,
  },
  {
    id: 409,
    name: 'OnePlus 13',
    brand: 'OnePlus',
    category: 'dien-thoai',
    categoryName: 'Điện thoại',
    price: 16990000,
    originalPrice: 18990000,
    rating: 4.6,
    reviewCount: 95,
    description: 'Sạc nhanh 100W, hiệu năng mượt mà, giá cạnh tranh.',
    specs: { 'Màn hình': '6.82 inch', 'Chip': 'Snapdragon 8 Gen 4 Leading', 'Pin': '6500mAh' },
    variants: [
      { color: 'Xám', storage: '256GB', price: 16990000, originalPrice: 18990000, stock: 12, image: 'https://placehold.co/800x800/4b5563/ffffff?text=OnePlus+13' },
      { color: 'Trắng', storage: '512GB', price: 19990000, originalPrice: 21990000, stock: 9, image: 'https://placehold.co/800x800/4b5563/ffffff?text=OnePlus+13' },
    ],
    images: [
      'https://placehold.co/800x800/4b5563/ffffff?text=OnePlus+13',
      'https://placehold.co/800x800/6b7280/ffffff?text=100W+Charging',
    ],
    image: 'https://placehold.co/800x800/4b5563/ffffff?text=OnePlus+13',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 410,
    name: 'Xiaomi 15 Ultra',
    brand: 'Xiaomi',
    category: 'dien-thoai',
    categoryName: 'Điện thoại',
    price: 24990000,
    originalPrice: 26990000,
    rating: 4.8,
    reviewCount: 134,
    description: 'Camera Leica 200MP, đơn vị pixel siêu lớn, zoom quang học 5.2x.',
    specs: { 'Màn hình': '6.73 inch', 'Chip': 'Snapdragon 8 Gen 4', 'Pin': '6500mAh' },
    variants: [
      { color: 'Đen', storage: '512GB', price: 24990000, originalPrice: 26990000, stock: 8, image: 'https://placehold.co/800x800/111827/ffffff?text=15+Ultra' },
      { color: 'Bạc', storage: '1TB', price: 27990000, originalPrice: 29990000, stock: 4, image: 'https://placehold.co/800x800/111827/ffffff?text=15+Ultra' },
    ],
    images: [
      'https://placehold.co/800x800/111827/ffffff?text=15+Ultra',
      'https://placehold.co/800x800/374151/ffffff?text=Leica+Camera',
    ],
    image: 'https://placehold.co/800x800/111827/ffffff?text=15+Ultra',
    isFeatured: true,
    isOnSale: true,
    isHot: true,
  },
  {
    id: 411,
    name: 'Samsung Galaxy Tab S9 FE',
    brand: 'Samsung',
    category: 'may-tinh-bang',
    categoryName: 'Máy tính bảng',
    price: 11990000,
    originalPrice: 13990000,
    rating: 4.5,
    reviewCount: 63,
    description: 'Giá rẻ, màn hình lớn 90Hz, pin lâu.',
    specs: { 'Màn hình': '13 inch', 'Chip': 'MediaTek Helio', 'Pin': '8000mAh' },
    variants: [
      { color: 'Xám', storage: '64GB', price: 11990000, originalPrice: 13990000, stock: 10, image: 'https://placehold.co/800x800/4b5563/ffffff?text=Tab+S9+FE' },
    ],
    images: [
      'https://placehold.co/800x800/4b5563/ffffff?text=Tab+S9+FE',
    ],
    image: 'https://placehold.co/800x800/4b5563/ffffff?text=Tab+S9+FE',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 412,
    name: 'iPad mini 7',
    brand: 'Apple',
    category: 'may-tinh-bang',
    categoryName: 'Máy tính bảng',
    price: 12990000,
    originalPrice: 14990000,
    rating: 4.8,
    reviewCount: 108,
    description: 'Nhỏ gọn, mạnh mẽ với chip A17 Pro, hoàn hảo cho di động.',
    specs: { 'Màn hình': '8.3 inch', 'Chip': 'A17 Pro', 'Pin': '19 giờ' },
    variants: [
      { color: 'Bạc', storage: '128GB', price: 12990000, originalPrice: 14990000, stock: 8, image: 'https://placehold.co/800x800/d1d5db/ffffff?text=iPad+mini+7' },
      { color: 'Xám', storage: '256GB', price: 15990000, originalPrice: 17990000, stock: 5, image: 'https://placehold.co/800x800/d1d5db/ffffff?text=iPad+mini+7' },
    ],
    images: [
      'https://placehold.co/800x800/d1d5db/ffffff?text=iPad+mini+7',
    ],
    image: 'https://placehold.co/800x800/d1d5db/ffffff?text=iPad+mini+7',
    isFeatured: true,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 413,
    name: 'Lenovo Tab M11',
    brand: 'Lenovo',
    category: 'may-tinh-bang',
    categoryName: 'Máy tính bảng',
    price: 9990000,
    originalPrice: 11990000,
    rating: 4.4,
    reviewCount: 54,
    description: 'Giá tốt, màn hình 11.5 inch, loa stereo kép.',
    specs: { 'Màn hình': '11.5 inch', 'Chip': 'MediaTek Helio G99', 'Pin': '8000mAh' },
    variants: [
      { color: 'Xám', storage: '128GB', price: 9990000, originalPrice: 11990000, stock: 12, image: 'https://placehold.co/800x800/6b7280/ffffff?text=Tab+M11' },
    ],
    images: [
      'https://placehold.co/800x800/6b7280/ffffff?text=Tab+M11',
    ],
    image: 'https://placehold.co/800x800/6b7280/ffffff?text=Tab+M11',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 414,
    name: 'OnePlus Pad 2',
    brand: 'OnePlus',
    category: 'may-tinh-bang',
    categoryName: 'Máy tính bảng',
    price: 14990000,
    originalPrice: 16990000,
    rating: 4.6,
    reviewCount: 82,
    description: 'Sạc nhanh 67W, màn hình 144Hz, ứng dụng HyperOS tối ưu.',
    specs: { 'Màn hình': '12.1 inch', 'Chip': 'Snapdragon 8 Gen 3', 'Pin': '9510mAh' },
    variants: [
      { color: 'Xám', storage: '256GB', price: 14990000, originalPrice: 16990000, stock: 7, image: 'https://placehold.co/800x800/4b5563/ffffff?text=OnePlus+Pad+2' },
    ],
    images: [
      'https://placehold.co/800x800/4b5563/ffffff?text=OnePlus+Pad+2',
    ],
    image: 'https://placehold.co/800x800/4b5563/ffffff?text=OnePlus+Pad+2',
    isFeatured: true,
    isOnSale: true,
    isHot: true,
  },
  {
    id: 415,
    name: 'Xiaomi Pad 7',
    brand: 'Xiaomi',
    category: 'may-tinh-bang',
    categoryName: 'Máy tính bảng',
    price: 10990000,
    originalPrice: 12990000,
    rating: 4.5,
    reviewCount: 76,
    description: 'Màn hình OLED 144Hz, giá rẻ, hiệu suất tốt.',
    specs: { 'Màn hình': '13.2 inch', 'Chip': 'Snapdragon 8 Gen 3 Leading', 'Pin': '10100mAh' },
    variants: [
      { color: 'Xám', storage: '256GB', price: 10990000, originalPrice: 12990000, stock: 9, image: 'https://placehold.co/800x800/4b5563/ffffff?text=Pad+7' },
    ],
    images: [
      'https://placehold.co/800x800/4b5563/ffffff?text=Pad+7',
    ],
    image: 'https://placehold.co/800x800/4b5563/ffffff?text=Pad+7',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 416,
    name: 'Samsung Galaxy Tab S6 Lite',
    brand: 'Samsung',
    category: 'may-tinh-bang',
    categoryName: 'Máy tính bảng',
    price: 8990000,
    originalPrice: 10990000,
    rating: 4.3,
    reviewCount: 48,
    description: 'Máy tính bảng phổ thông, màn hình 10.4 inch, chính xác.',
    specs: { 'Màn hình': '10.4 inch', 'Chip': 'MediaTek Helio G99', 'Pin': '7000mAh' },
    variants: [
      { color: 'Xám', storage: '64GB', price: 8990000, originalPrice: 10990000, stock: 11, image: 'https://placehold.co/800x800/6b7280/ffffff?text=Tab+S6' },
    ],
    images: [
      'https://placehold.co/800x800/6b7280/ffffff?text=Tab+S6',
    ],
    image: 'https://placehold.co/800x800/6b7280/ffffff?text=Tab+S6',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 417,
    name: 'Sạc nhanh 35W Anker',
    brand: 'Anker',
    category: 'phu-kien',
    categoryName: 'Phụ kiện',
    price: 590000,
    originalPrice: 690000,
    rating: 4.8,
    reviewCount: 187,
    description: 'Sạc nhanh công suất cao, an toàn, giá phải chăng.',
    specs: { 'Công suất': '35W', 'Cổng': 'USB-C', 'Tương thích': 'Toàn cầu' },
    variants: [
      { color: 'Trắng', storage: '', price: 590000, originalPrice: 690000, stock: 40, image: 'https://placehold.co/800x800/ffffff/000000?text=Anker+35W' },
    ],
    images: [
      'https://placehold.co/800x800/ffffff/000000?text=Anker+35W',
    ],
    image: 'https://placehold.co/800x800/ffffff/000000?text=Anker+35W',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 418,
    name: 'Cáp Lightning Apple',
    brand: 'Apple',
    category: 'phu-kien',
    categoryName: 'Phụ kiện',
    price: 890000,
    originalPrice: 990000,
    rating: 4.7,
    reviewCount: 234,
    description: 'Cáp chính hãng Apple, chất lượng đảm bảo, bền bỉ.',
    specs: { 'Dài': '2m', 'Tốc độ': 'MFi certified', 'Chất liệu': 'Durable' },
    variants: [
      { color: 'Trắng', storage: '', price: 890000, originalPrice: 990000, stock: 35, image: 'https://placehold.co/800x800/f5f5f5/0f172a?text=Lightning+Cable' },
    ],
    images: [
      'https://placehold.co/800x800/f5f5f5/0f172a?text=Lightning+Cable',
    ],
    image: 'https://placehold.co/800x800/f5f5f5/0f172a?text=Lightning+Cable',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 419,
    name: 'Ốp lưng iPhone 16 Pro',
    brand: 'Spigen',
    category: 'phu-kien',
    categoryName: 'Phụ kiện',
    price: 290000,
    originalPrice: 390000,
    rating: 4.6,
    reviewCount: 156,
    description: 'Ốp lưng bảo vệ tốt, vừa vặn, nhiều màu lựa chọn.',
    specs: { 'Chất liệu': 'TPU + PC', 'Độ dầy': '1.5mm', 'Tương thích': 'iPhone 16 Pro' },
    variants: [
      { color: 'Đen', storage: '', price: 290000, originalPrice: 390000, stock: 45, image: 'https://placehold.co/800x800/111827/ffffff?text=iPhone+Case' },
      { color: 'Xanh', storage: '', price: 290000, originalPrice: 390000, stock: 30, image: 'https://placehold.co/800x800/111827/ffffff?text=iPhone+Case' },
    ],
    images: [
      'https://placehold.co/800x800/111827/ffffff?text=iPhone+Case',
    ],
    image: 'https://placehold.co/800x800/111827/ffffff?text=iPhone+Case',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 420,
    name: 'Pin sạc dự phòng 20000mAh',
    brand: 'Anker',
    category: 'phu-kien',
    categoryName: 'Phụ kiện',
    price: 890000,
    originalPrice: 1090000,
    rating: 4.7,
    reviewCount: 198,
    description: 'Pin dự phòng dùng được lâu, sạc nhanh, hiển thị LED.',
    specs: { 'Dung lượng': '20000mAh', 'Sạc': '22W', 'Cổng': 'USB-C' },
    variants: [
      { color: 'Đen', storage: '', price: 890000, originalPrice: 1090000, stock: 28, image: 'https://placehold.co/800x800/111827/ffffff?text=Power+Bank' },
    ],
    images: [
      'https://placehold.co/800x800/111827/ffffff?text=Power+Bank',
    ],
    image: 'https://placehold.co/800x800/111827/ffffff?text=Power+Bank',
    isFeatured: true,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 421,
    name: 'Loa Bluetooth JBL Flip 7',
    brand: 'JBL',
    category: 'phu-kien',
    categoryName: 'Phụ kiện',
    price: 2990000,
    originalPrice: 3490000,
    rating: 4.8,
    reviewCount: 142,
    description: 'Loa di động, chống nước, âm thanh sống động.',
    specs: { 'Công suất': '20W', 'Thời lượng': '12 giờ', 'Chống nước': 'IPX67' },
    variants: [
      { color: 'Đen', storage: '', price: 2990000, originalPrice: 3490000, stock: 15, image: 'https://placehold.co/800x800/111827/ffffff?text=JBL+Flip+7' },
      { color: 'Xanh', storage: '', price: 2990000, originalPrice: 3490000, stock: 12, image: 'https://placehold.co/800x800/111827/ffffff?text=JBL+Flip+7' },
    ],
    images: [
      'https://placehold.co/800x800/111827/ffffff?text=JBL+Flip+7',
    ],
    image: 'https://placehold.co/800x800/111827/ffffff?text=JBL+Flip+7',
    isFeatured: true,
    isOnSale: true,
    isHot: true,
  },
  {
    id: 422,
    name: 'Chuột không dây Logitech M705',
    brand: 'Logitech',
    category: 'phu-kien',
    categoryName: 'Phụ kiện',
    price: 890000,
    originalPrice: 1090000,
    rating: 4.6,
    reviewCount: 124,
    description: 'Chuột không dây, pin lâu, thoải mái sử dụng dài lâu.',
    specs: { 'Kết nối': 'Wireless 2.4GHz', 'Pin': '36 tháng', 'Nút': '8 nút' },
    variants: [
      { color: 'Đen', storage: '', price: 890000, originalPrice: 1090000, stock: 22, image: 'https://placehold.co/800x800/111827/ffffff?text=Logitech+M705' },
    ],
    images: [
      'https://placehold.co/800x800/111827/ffffff?text=Logitech+M705',
    ],
    image: 'https://placehold.co/800x800/111827/ffffff?text=Logitech+M705',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 423,
    name: 'Kính cường lực iPhone 16',
    brand: 'Spigen',
    category: 'phu-kien',
    categoryName: 'Phụ kiện',
    price: 190000,
    originalPrice: 290000,
    rating: 4.7,
    reviewCount: 201,
    description: 'Kính cường lực 9H, cài dễ, độ trong suốt cao.',
    specs: { 'Độ cứng': '9H', 'Độ dầy': '0.33mm', 'Tương thích': 'iPhone 16' },
    variants: [
      { color: 'Trong suốt', storage: '', price: 190000, originalPrice: 290000, stock: 60, image: 'https://placehold.co/800x800/ffffff/d1d5db?text=Tempered+Glass' },
    ],
    images: [
      'https://placehold.co/800x800/ffffff/d1d5db?text=Tempered+Glass',
    ],
    image: 'https://placehold.co/800x800/ffffff/d1d5db?text=Tempered+Glass',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 424,
    name: 'Bao da iPad Air',
    brand: 'Apple',
    category: 'phu-kien',
    categoryName: 'Phụ kiện',
    price: 1190000,
    originalPrice: 1390000,
    rating: 4.5,
    reviewCount: 87,
    description: 'Bao da chính hãng Apple, bảo vệ toàn diện, kiểu dáng sang trọng.',
    specs: { 'Chất liệu': 'Polyurethane', 'Tương thích': 'iPad Air M2', 'Màu': 'Navy Blue' },
    variants: [
      { color: 'Navy Blue', storage: '', price: 1190000, originalPrice: 1390000, stock: 18, image: 'https://placehold.co/800x800/001f3f/ffffff?text=iPad+Case' },
    ],
    images: [
      'https://placehold.co/800x800/001f3f/ffffff?text=iPad+Case',
    ],
    image: 'https://placehold.co/800x800/001f3f/ffffff?text=iPad+Case',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 425,
    name: 'Tai nghe không dây Samsung Galaxy Buds3',
    brand: 'Samsung',
    category: 'phu-kien',
    categoryName: 'Phụ kiện',
    price: 3290000,
    originalPrice: 3890000,
    rating: 4.7,
    reviewCount: 165,
    description: 'Tai nghe true wireless, chống ồn tốt, kết nối nhanh.',
    specs: { 'Kết nối': 'Bluetooth 5.4', 'Pin': '8 giờ', 'Chống ồn': 'ANC' },
    variants: [
      { color: 'Bạc', storage: '', price: 3290000, originalPrice: 3890000, stock: 14, image: 'https://placehold.co/800x800/c0c0c0/333333?text=Galaxy+Buds3' },
    ],
    images: [
      'https://placehold.co/800x800/c0c0c0/333333?text=Galaxy+Buds3',
    ],
    image: 'https://placehold.co/800x800/c0c0c0/333333?text=Galaxy+Buds3',
    isFeatured: true,
    isOnSale: true,
    isHot: true,
  },
  {
    id: 426,
    name: 'Cáp sạc USB-C 2m Anker',
    brand: 'Anker',
    category: 'phu-kien',
    categoryName: 'Phụ kiện',
    price: 290000,
    originalPrice: 390000,
    rating: 4.6,
    reviewCount: 178,
    description: 'Cáp USB-C chất lượng cao, tương thích đa thiết bị.',
    specs: { 'Dài': '2m', 'Tốc độ': '10Gbps data', 'Tối đa': '100W' },
    variants: [
      { color: 'Đen', storage: '', price: 290000, originalPrice: 390000, stock: 50, image: 'https://placehold.co/800x800/111827/ffffff?text=USB-C+Cable' },
    ],
    images: [
      'https://placehold.co/800x800/111827/ffffff?text=USB-C+Cable',
    ],
    image: 'https://placehold.co/800x800/111827/ffffff?text=USB-C+Cable',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 427,
    name: 'Lenovo ThinkPad X1 Nano',
    brand: 'Lenovo',
    category: 'laptop',
    categoryName: 'Laptop',
    price: 19990000,
    originalPrice: 21990000,
    rating: 4.8,
    reviewCount: 73,
    description: 'Laptop siêu mỏng nhẹ, đầy đủ kết nối, pin lâu.',
    specs: { 'Màn hình': '13.3 inch', 'Chip': 'Intel Core i5', 'RAM': '16GB' },
    variants: [
      { color: 'Đen', storage: '512GB SSD', price: 19990000, originalPrice: 21990000, stock: 7, image: 'https://placehold.co/800x800/111827/ffffff?text=ThinkPad+X1' },
    ],
    images: [
      'https://placehold.co/800x800/111827/ffffff?text=ThinkPad+X1',
    ],
    image: 'https://placehold.co/800x800/111827/ffffff?text=ThinkPad+X1',
    isFeatured: true,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 428,
    name: 'HP Envy 16',
    brand: 'HP',
    category: 'laptop',
    categoryName: 'Laptop',
    price: 21990000,
    originalPrice: 23990000,
    rating: 4.7,
    reviewCount: 61,
    description: 'Màn hình OLED 16 inch, hiệu suất cao, thiết kế đẹp.',
    specs: { 'Màn hình': '16 inch OLED', 'Chip': 'Intel Core i7', 'RAM': '16GB' },
    variants: [
      { color: 'Bạc', storage: '512GB SSD', price: 21990000, originalPrice: 23990000, stock: 6, image: 'https://placehold.co/800x800/c0c0c0/333333?text=HP+Envy+16' },
    ],
    images: [
      'https://placehold.co/800x800/c0c0c0/333333?text=HP+Envy+16',
    ],
    image: 'https://placehold.co/800x800/c0c0c0/333333?text=HP+Envy+16',
    isFeatured: true,
    isOnSale: true,
    isHot: true,
  },
  {
    id: 429,
    name: 'ASUS ZenBook 13 OLED',
    brand: 'ASUS',
    category: 'laptop',
    categoryName: 'Laptop',
    price: 18990000,
    originalPrice: 20990000,
    rating: 4.6,
    reviewCount: 52,
    description: 'Laptop mỏng nhẹ, màn hình OLED đẹp, pin lâu.',
    specs: { 'Màn hình': '13.3 inch OLED', 'Chip': 'Intel Core i5', 'RAM': '8GB' },
    variants: [
      { color: 'Xanh', storage: '512GB SSD', price: 18990000, originalPrice: 20990000, stock: 9, image: 'https://placehold.co/800x800/1d4ed8/ffffff?text=ZenBook+13' },
    ],
    images: [
      'https://placehold.co/800x800/1d4ed8/ffffff?text=ZenBook+13',
    ],
    image: 'https://placehold.co/800x800/1d4ed8/ffffff?text=ZenBook+13',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
  {
    id: 430,
    name: 'Dell Vostro 15 3000',
    brand: 'Dell',
    category: 'laptop',
    categoryName: 'Laptop',
    price: 14990000,
    originalPrice: 16990000,
    rating: 4.4,
    reviewCount: 46,
    description: 'Laptop tầm trung cho học tập và công việc văn phòng.',
    specs: { 'Màn hình': '15.6 inch', 'Chip': 'Intel Core i5', 'RAM': '8GB' },
    variants: [
      { color: 'Đen', storage: '256GB SSD', price: 14990000, originalPrice: 16990000, stock: 11, image: 'https://placehold.co/800x800/111827/ffffff?text=Vostro+15' },
    ],
    images: [
      'https://placehold.co/800x800/111827/ffffff?text=Vostro+15',
    ],
    image: 'https://placehold.co/800x800/111827/ffffff?text=Vostro+15',
    isFeatured: false,
    isOnSale: true,
    isHot: false,
  },
];

const DEMO_BANNERS = [
  {
    id: 1,
    title: 'Siêu sale iPhone và flagship',
    imageUrl: 'https://placehold.co/1200x520/1a1a2e/ffffff?text=Big+Sale+%E2%80%93+PhoneStore',
    linkUrl: 'products.html?cat=dien-thoai',
    displayOrder: 1,
    isActive: true,
  },
  {
    id: 2,
    title: 'Laptop học tập giá tốt',
    imageUrl: 'https://placehold.co/1200x520/0f172a/ffffff?text=Laptop+Deals',
    linkUrl: 'products.html?cat=laptop',
    displayOrder: 2,
    isActive: true,
  },
  {
    id: 3,
    title: 'Phụ kiện chính hãng',
    imageUrl: 'https://placehold.co/1200x520/111827/ffffff?text=Accessories+Promo',
    linkUrl: 'products.html?cat=phu-kien',
    displayOrder: 3,
    isActive: true,
  },
];

const catalogStatus = {
  loaded: false,
  success: false,
  error: null,
};

function getCategoryIcon(value) {
  const key = String(value || '').toLowerCase();
  if (key.includes('dien-thoai')) return '📱';
  if (key.includes('tablet')) return '📟';
  if (key.includes('laptop')) return '💻';
  if (key.includes('am-thanh') || key.includes('mic')) return '🎧';
  if (key.includes('dong-ho')) return '⌚';
  if (key.includes('phu-kien')) return '🎒';
  if (key.includes('pc') || key.includes('man-hinh') || key.includes('may-in')) return '🖥️';
  if (key.includes('tivi')) return '📺';
  return '✨';
}

async function fetchJson(path, options = {}) {
  const response = await fetch(`${PHONESTORE_API_BASE}${path}`, options);
  if (!response.ok) {
    let detail = '';
    try {
      detail = await response.text();
    } catch {
      detail = '';
    }
    throw new Error(`API ${path} failed: ${response.status}${detail ? ` - ${detail}` : ''}`);
  }
  return response.json();
}

function normalizeCategory(item) {
  const id = item.id || item.slug || item.categoryID || '';
  return {
    id,
    name: item.name || item.categoryName || item.CategoryName || '',
    icon: item.icon || getCategoryIcon(id || item.name || item.categoryName),
    count: Number(item.count ?? item.productCount ?? item.ProductCount ?? 0),
    description: item.description || item.Description || '',
  };
}

function normalizeVariant(variant = {}) {
  return {
    id: variant.id || variant.variantID || variant.VariantID || 0,
    color: variant.color || variant.Color || '',
    storage: variant.storage || variant.Storage || '',
    price: Number(variant.price ?? variant.Price ?? 0),
    originalPrice: variant.originalPrice ? Number(variant.originalPrice) : Number(variant.OriginalPrice || 0) || null,
    stock: Number(variant.stock ?? variant.StockQuantity ?? 0),
    sku: variant.sku || variant.SKU || '',
    image: variant.image || variant.ImageURL || '',
  };
}

function normalizeProduct(item) {
  if (!item) return null;
  const variants = (item.variants || item.Variants || []).map(normalizeVariant);
  const images = item.images || item.Images || variants.map(v => v.image).filter(Boolean);
  const price = Number(item.price ?? item.Price ?? (variants[0]?.price || 0));
  const originalPrice = Number(item.originalPrice ?? item.OriginalPrice ?? (variants[0]?.originalPrice || price));
  const discount = Number(item.discount ?? item.Discount ?? (originalPrice > price ? Math.round((1 - price / originalPrice) * 100) : 0));

  return {
    id: Number(item.id ?? item.productID ?? item.ProductID),
    name: item.name || item.productName || item.ProductName || '',
    slug: item.slug || item.Slug || '',
    brand: item.brand || item.brandName || item.BrandName || '',
    category: item.category || item.categorySlug || item.CategorySlug || '',
    categoryName: item.categoryName || item.CategoryName || '',
    description: item.description || item.fullDescription || item.FullDescription || item.shortDescription || item.ShortDescription || '',
    specs: item.specs || item.Specifications || {},
    warranty: item.warranty || item.Warranty || '',
    rating: Number(item.rating ?? item.avgRating ?? item.AvgRating ?? 4.6),
    reviewCount: Number(item.reviewCount ?? item.ReviewCount ?? 0),
    price,
    originalPrice: originalPrice < price ? price : originalPrice,
    discount,
    image: item.image || images[0] || '',
    images,
    variants,
    isFeatured: Boolean(item.isFeatured ?? item.IsFeatured),
    isOnSale: Boolean(item.isOnSale ?? item.IsOnSale ?? discount > 0),
    isHot: Boolean(item.isHot ?? item.IsHot ?? discount >= 15),
    tags: item.tags || item.Tags || [],
  };
}

function normalizeBanner(item) {
  return {
    id: Number(item.id ?? item.bannerID ?? item.BannerID ?? 0),
    title: item.title || item.Title || '',
    imageUrl: item.imageUrl || item.imageURL || item.ImageURL || '',
    linkUrl: item.linkUrl || item.linkURL || item.LinkURL || '#',
    displayOrder: Number(item.displayOrder ?? item.DisplayOrder ?? 0),
    isActive: Boolean(item.isActive ?? item.IsActive ?? true),
  };
}

function applyDemoCatalog() {
  categories = DEMO_CATEGORIES.map(normalizeCategory);
  brands = DEMO_BRANDS.slice();
  products = DEMO_PRODUCTS.map(normalizeProduct).filter(Boolean);
  banners = DEMO_BANNERS.map(normalizeBanner);

  catalogStatus.loaded = true;
  catalogStatus.success = true;
  catalogStatus.error = null;

  return {
    success: true,
    categories,
    brands,
    products,
  };
}

async function loadCatalog() {
  if (catalogPromise) return catalogPromise;

  catalogPromise = (async () => {
    try {
      const [categoryData, brandData, productData] = await Promise.all([
        fetchJson('/Product/GetCategories'),
        fetchJson('/Product/GetBrands'),
        fetchJson('/Product/GetProducts'),
      ]);

      categories = Array.isArray(categoryData) ? categoryData.map(normalizeCategory) : [];
      brands = Array.isArray(brandData) ? brandData.slice() : [];
      products = Array.isArray(productData) ? productData.map(normalizeProduct).filter(Boolean) : [];

      if (!categories.length || !brands.length || !products.length) {
        console.warn('Catalog MVC returned empty data, using demo data.');
        return applyDemoCatalog();
      }

      catalogStatus.loaded = true;
      catalogStatus.success = true;
      catalogStatus.error = null;

      return {
        success: true,
        categories,
        brands,
        products,
      };
    } catch (error) {
      console.warn('Catalog MVC unavailable, using demo data:', error);
      return applyDemoCatalog();
    }
  })();

  return catalogPromise;
}

async function loadBanners() {
  try {
    const data = await fetchJson('/banners');
    banners = Array.isArray(data) ? data.map(normalizeBanner) : [];
    if (!banners.length) {
      console.warn('Banner API returned empty data, using demo banners.');
      banners = DEMO_BANNERS.map(normalizeBanner);
    }
    return { success: true, banners };
  } catch (error) {
    console.warn('Banner API unavailable, using demo banners:', error);
    banners = DEMO_BANNERS.map(normalizeBanner);
    return { success: true, banners };
  }
}

function getCatalogStatus() {
  return { ...catalogStatus };
}

function formatPrice(price) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
  }).format(price || 0);
}

function getProductById(id) {
  return products.find(p => p.id === parseInt(id, 10));
}

function getProductsByCategory(categoryId, filters = {}) {
  let result = products.filter(p => p.category === categoryId);

  if (filters.brand && filters.brand.length > 0) {
    result = result.filter(p => filters.brand.includes(p.brand));
  }

  if (filters.minPrice !== undefined) {
    result = result.filter(p => p.price >= filters.minPrice);
  }

  if (filters.maxPrice !== undefined) {
    result = result.filter(p => p.price <= filters.maxPrice);
  }

  if (filters.sort === 'price-asc') {
    result.sort((a, b) => a.price - b.price);
  } else if (filters.sort === 'price-desc') {
    result.sort((a, b) => b.price - a.price);
  } else if (filters.sort === 'popular') {
    result.sort((a, b) => b.reviewCount - a.reviewCount);
  } else if (filters.sort === 'rating') {
    result.sort((a, b) => b.rating - a.rating);
  }

  return result;
}

function searchProducts(keyword) {
  const kw = String(keyword || '').toLowerCase().trim();
  if (!kw) return [];
  return products.filter(p =>
    (p.name || '').toLowerCase().includes(kw)
    || (p.brand || '').toLowerCase().includes(kw)
    || (p.description || '').toLowerCase().includes(kw)
  );
}

function getFeaturedProducts() {
  const featured = products.filter(p => p.isFeatured);
  return featured.length ? featured : products.slice(0, 8);
}

function getSaleProducts() {
  return products.filter(p => p.isOnSale && p.discount >= 10);
}

function getRelatedProducts(product) {
  return products
    .filter(p => p.category === product.category && p.id !== product.id)
    .slice(0, 4);
}

window.loadCatalog = loadCatalog;
window.loadBanners = loadBanners;
window.getCatalogStatus = getCatalogStatus;
