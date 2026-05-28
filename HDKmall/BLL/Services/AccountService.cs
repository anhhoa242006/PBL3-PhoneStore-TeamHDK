using HDKmall.BLL.Interfaces;
using HDKmall.DAL.Interfaces;
using HDKmall.Models;
using HDKmall.ViewModels;
using System.Linq;
using System.Collections.Generic;
namespace HDKmall.BLL.Services
{   
    public class AccountService : IAccountService
    {
        private readonly IUserRepository _userRepository;

        public AccountService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public User Authenticate(LoginVM model)
        {
            var user = _userRepository.GetUserByEmail(model.Email);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !user.IsActive) return null;

            if (!IsPasswordValid(model.Password, user.PasswordHash))
            {
                return null;
            }

            if (user.PasswordHash == model.Password)
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                _userRepository.UpdateUser(user);
                _userRepository.SaveChanges();
            }

            return user;
        }

        public User GetUserByEmail(string email)
        {
            return _userRepository.GetUserByEmail(email);
        }

        public bool RegisterUser(RegisterVM model)
        {
            var existingUser = _userRepository.GetUserByEmail(model.Email);
            if (existingUser != null) return false;

            var newUser = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                RoleId = _userRepository.GetCustomerRoleId(), // Lấy RoleID của Customer
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _userRepository.AddUser(newUser);
            _userRepository.SaveChanges();
            return true;
        }

        public ProfileVM GetProfile(int userId)
        {
            var user = _userRepository.GetUserById(userId);
            if (user == null) return null;

            var addresses = _userRepository.GetUserAddressesByUserId(userId);
            return new ProfileVM
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Addresses = addresses.Select(a => new AddressItemVM
                {
                    AddressId = a.AddressId,
                    RecipientName = a.RecipientName,
                    PhoneNumber = a.PhoneNumber,
                    AddressLine = a.AddressLine,
                    IsDefault = a.IsDefault
                }).ToList()
            };
        }

        public bool UpdateProfile(int userId, ProfileVM model)
        {
            var user = _userRepository.GetUserById(userId);
            if (user == null) return false;

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            _userRepository.UpdateUser(user);
            _userRepository.SaveChanges();
            return true;
        }

        public bool ChangePassword(int userId, string currentPassword, string newPassword)
        {
            var user = _userRepository.GetUserById(userId);
            if (user == null || string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return false;
            }

            if (!IsPasswordValid(currentPassword, user.PasswordHash))
            {
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _userRepository.UpdateUser(user);
            _userRepository.SaveChanges();
            return true;
        }

        public bool AddAddress(int userId, ProfileVM model)
        {
            if (string.IsNullOrWhiteSpace(model.NewAddressRecipientName) ||
                string.IsNullOrWhiteSpace(model.NewAddressPhoneNumber) ||
                string.IsNullOrWhiteSpace(model.NewAddressLine))
            {
                return false;
            }

            var address = new UserAddress
            {
                UserId = userId,
                RecipientName = model.NewAddressRecipientName.Trim(),
                PhoneNumber = model.NewAddressPhoneNumber.Trim(),
                AddressLine = model.NewAddressLine.Trim(),
                IsDefault = model.SetAsDefaultAddress
            };

            if (address.IsDefault)
            {
                var addresses = _userRepository.GetUserAddressesByUserId(userId);
                foreach (var existingAddress in addresses.Where(a => a.IsDefault))
                {
                    existingAddress.IsDefault = false;
                }
            }

            _userRepository.AddUserAddress(address);
            _userRepository.SaveChanges();
            return true;
        }

        public bool DeleteAddress(int userId, int addressId)
        {
            var address = _userRepository.GetUserAddressById(userId, addressId);
            if (address == null) return false;

            var wasDefault = address.IsDefault;
            _userRepository.DeleteUserAddress(address);
            _userRepository.SaveChanges();

            if (wasDefault)
            {
                var firstAddress = _userRepository.GetUserAddressesByUserId(userId).FirstOrDefault();
                if (firstAddress != null)
                {
                    firstAddress.IsDefault = true;
                    _userRepository.SaveChanges();
                }
            }

            return true;
        }

        public bool SetDefaultAddress(int userId, int addressId)
        {
            var selectedAddress = _userRepository.GetUserAddressById(userId, addressId);
            if (selectedAddress == null) return false;

            var addresses = _userRepository.GetUserAddressesByUserId(userId);
            foreach (var address in addresses)
            {
                address.IsDefault = address.AddressId == addressId;
            }

            _userRepository.SaveChanges();
            return true;
        }

        private static bool IsPasswordValid(string password, string passwordHash)
        {
            if (passwordHash == password)
            {
                return true;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _userRepository.GetAllUsers();
        }

        public void ToggleUserStatus(int userId)
        {
            var user = _userRepository.GetUserById(userId);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                _userRepository.UpdateUser(user);
                _userRepository.SaveChanges();
            }
        }

        public void ChangeUserRole(int userId, int roleId)
        {
            var user = _userRepository.GetUserById(userId);
            if (user != null)
            {
                user.RoleId = roleId;
                _userRepository.UpdateUser(user);
                _userRepository.SaveChanges();
            }
        }

        public IEnumerable<Role> GetAllRoles()
        {
            return _userRepository.GetAllRoles();
        }

        public async Task<string?> GeneratePasswordResetTokenAsync(string email)
        {
            var user = _userRepository.GetUserByEmail(email);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            var token = Guid.NewGuid().ToString();
            user.ResetPasswordToken = token;
            user.ResetPasswordTokenExpiry = DateTime.Now.AddMinutes(15);
            
            _userRepository.UpdateUser(user);
            _userRepository.SaveChanges();

            return await Task.FromResult(token);
        }

        public bool ValidatePasswordResetToken(string email, string token)
        {
            var user = _userRepository.GetUserByEmail(email);
            if (user == null || user.ResetPasswordToken != token || user.ResetPasswordTokenExpiry == null || user.ResetPasswordTokenExpiry < DateTime.Now)
            {
                return false;
            }
            return true;
        }

        public bool ResetPassword(string email, string token, string newPassword)
        {
            var user = _userRepository.GetUserByEmail(email);
            if (user == null || user.ResetPasswordToken != token || user.ResetPasswordTokenExpiry == null || user.ResetPasswordTokenExpiry < DateTime.Now)
            {
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordTokenExpiry = null;

            _userRepository.UpdateUser(user);
            _userRepository.SaveChanges();
            
            return true;
        }
    }
}
