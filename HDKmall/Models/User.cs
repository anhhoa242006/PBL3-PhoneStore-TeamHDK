using System;

namespace HDKmall.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PasswordHash { get; set; }
        
        public int RoleId { get; set; }
        public Role? Role { get; set; }
        public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();

        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
