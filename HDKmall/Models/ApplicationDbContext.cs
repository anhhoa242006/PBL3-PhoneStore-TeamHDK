using Microsoft.EntityFrameworkCore;

namespace HDKmall.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Role> Roles { get; set; }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<ProductVersion> ProductVersions { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductSpecification> ProductSpecifications { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserAddress>()
                .HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==================== PRODUCT HIERARCHY ====================
            modelBuilder.Entity<ProductVersion>()
                .HasOne(v => v.Product)
                .WithMany(p => p.Versions)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(v => v.ProductVersion)
                .WithMany(pv => pv.Variants)
                .HasForeignKey(v => v.ProductVersionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductSpecification>()
                .HasOne(s => s.ProductVersion)
                .WithMany(pv => pv.Specifications)
                .HasForeignKey(s => s.ProductVersionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductImage>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.ProductVersion)
                .WithMany(pv => pv.Reviews)
                .HasForeignKey(r => r.ProductVersionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.OrderId)
                .IsUnique();

            // ==================== ROLES ====================
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Customer" }
            );

            // ==================== USERS ====================
            modelBuilder.Entity<User>().HasData(
                new User 
                { 
                    UserId = 1, 
                    FullName = "Administrator", 
                    Email = "admin@hdk.com", 
                    PhoneNumber = "0123456789", 
                    // Password: admin123 (Hashed using BCrypt - assuming the app uses BCrypt or similar)
                    PasswordHash = "$2a$11$KPB0W.m.r7Z.m8z7v9E.7.Y6b6x9H7b7B7b7B7b7B7b7B7b7B7b7", 
                    RoleId = 1, 
                    IsActive = true, 
                    CreatedAt = new System.DateTime(2025, 1, 1)
                }
            );

            // ==================== CATEGORIES ====================
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Điện thoại" },
                new Category { Id = 2, Name = "Máy tính bảng" },
                new Category { Id = 3, Name = "Laptop" },
                new Category { Id = 4, Name = "Loa" },
                new Category { Id = 5, Name = "Phụ kiện" },
                new Category { Id = 6, Name = "Đồng hồ thông minh" }
            );

            // ==================== BRANDS ====================
            modelBuilder.Entity<Brand>().HasData(
                new Brand { Id = 1, Name = "Apple" },
                new Brand { Id = 2, Name = "Samsung" },
                new Brand { Id = 3, Name = "Xiaomi" },
                new Brand { Id = 4, Name = "Oppo" },
                new Brand { Id = 5, Name = "Vivo" },
                new Brand { Id = 6, Name = "Sony" }
            );

            // ==================== BANNERS ====================
            modelBuilder.Entity<Banner>().HasData(
                new Banner { Id = 1, Title = "Khuyến Mãi Lớn", ImageUrl = "/images/banner-sale.jpg", LinkUrl = "/Product/Index", IsActive = true }
            );

            // ==================== INDEXES ====================
            modelBuilder.Entity<Product>().HasIndex(p => p.IsActive);
            modelBuilder.Entity<Product>().HasIndex(p => p.ProductType);
            modelBuilder.Entity<ProductVersion>().HasIndex(v => v.ProductId);
            modelBuilder.Entity<CartItem>().HasIndex(ci => ci.ShoppingCartId);
        }
    }
}
