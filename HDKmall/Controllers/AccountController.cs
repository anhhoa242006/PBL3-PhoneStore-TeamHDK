using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HDKmall.BLL.Interfaces;
using HDKmall.ViewModels;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Linq;

namespace HDKmall.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IEmailService _emailService;

        public AccountController(IAccountService accountService, IEmailService emailService)
        {
            _accountService = accountService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = _accountService.GetUserByEmail(model.Email);
                if (existingUser == null)
                {
                    ModelState.AddModelError("Email", "Tài khoản này chưa được đăng ký trên hệ thống.");
                    return View(model);
                }

                if (!existingUser.IsActive)
                {
                    ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ hỗ trợ.");
                    return View(model);
                }

                var user = _accountService.Authenticate(model);
                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Customer")
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                    TempData["success"] = "Chào mừng " + user.FullName + " đã quay trở lại!";
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("Password", "Mật khẩu không chính xác. Vui lòng thử lại.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, "Google");
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal == null)
            {
                Console.WriteLine("Google Auth Failed: result.Succeeded=" + result.Succeeded);
                TempData["ErrorMessage"] = "Đăng nhập bằng Google thất bại. Vui lòng thử lại.";
                return RedirectToAction("Login");
            }

            var email = result.Principal.FindFirstValue(ClaimTypes.Email);
            var name = result.Principal.FindFirstValue(ClaimTypes.Name);
            
            Console.WriteLine($"Google Auth Success: Email={email}, Name={name}");
            
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Không thể lấy thông tin Email từ tài khoản Google của bạn.";
                return RedirectToAction("Login");
            }

            var user = _accountService.GetUserByEmail(email);

            if (user == null)
            {
                // Tự động đăng ký nếu chưa có tài khoản
                var registerSuccess = _accountService.RegisterUser(new RegisterVM
                {
                    FullName = name ?? "Người dùng Google",
                    Email = email,
                    Password = "GoogleLogin_TempPassword!123", // Mật khẩu giả
                    ConfirmPassword = "GoogleLogin_TempPassword!123"
                });

                if (!registerSuccess)
                {
                    TempData["ErrorMessage"] = "Không thể tạo tài khoản từ Google. Email có thể đã tồn tại.";
                    return RedirectToAction("Login");
                }

                user = _accountService.GetUserByEmail(email);
            }
            
            if (user != null && !user.IsActive)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["ErrorMessage"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("Login");
            }

            if (user != null)
            {
                await RefreshIdentityAsync(user.UserId, user.FullName, user.Email);
                TempData["success"] = "Chào mừng " + user.FullName + " đã đăng nhập bằng Google!";
                return RedirectToAction("Index", "Home");
            }

            TempData["ErrorMessage"] = "Đã có lỗi xảy ra trong quá trình đăng nhập.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                Console.WriteLine("ModelState Error: " + errors);
                return View(model);
            }

            var success = _accountService.RegisterUser(model);
            if (success)
            {
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            ModelState.AddModelError("", "Email này đã được sử dụng.");

            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var token = await _accountService.GeneratePasswordResetTokenAsync(model.Email);
            if (token == null)
            {
                // Không hiển thị lỗi chi tiết để bảo mật, chỉ chuyển hướng sang trang xác nhận.
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var callbackUrl = Url.Action(
                "ResetPassword",
                "Account",
                new { email = model.Email, token },
                protocol: HttpContext.Request.Scheme);

            var subject = "Đặt lại mật khẩu HDKmall";
            var message = $@"
                <h3>Yêu cầu đặt lại mật khẩu</h3>
                <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản {model.Email}.</p>
                <p>Vui lòng click vào liên kết bên dưới để tạo mật khẩu mới. Liên kết này sẽ hết hạn sau 15 phút.</p>
                <p><a href='{callbackUrl}'>Đặt lại mật khẩu</a></p>
                <br>
                <p>Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>";

            try
            {
                await _emailService.SendEmailAsync(model.Email, subject, message);
                return RedirectToAction("ForgotPasswordConfirmation");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ForgotPassword] SMTP error: {ex.Message}");
                Console.WriteLine($"[DEVELOPMENT ONLY] Password Reset Link: {callbackUrl}");
                
                ModelState.AddModelError(string.Empty, "Không thể gửi email đặt lại mật khẩu do sự cố kết nối SMTP. Liên kết đặt lại mật khẩu đã được ghi nhận trong console hệ thống cho nhà phát triển.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() => View();

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            if (!_accountService.ValidatePasswordResetToken(email, token))
            {
                TempData["ErrorMessage"] = "Liên kết đã hết hạn hoặc không hợp lệ.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordVM { Email = email, Token = token };
            return View(model);
        }

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var success = _accountService.ResetPassword(model.Email, model.Token, model.NewPassword);
            if (!success)
            {
                ModelState.AddModelError("", "Liên kết đã hết hạn hoặc không hợp lệ.");
                return View(model);
            }

            return RedirectToAction("ResetPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation() => View();

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public IActionResult Profile()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return RedirectToAction("Login");

            var profile = _accountService.GetProfile(userId);
            if (profile == null) return RedirectToAction("Login");

            return View(profile);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileVM model)
        {
            if (!ModelState.IsValid)
            {
                model.Addresses = _accountService.GetProfile(GetCurrentUserId())?.Addresses ?? new List<AddressItemVM>();
                return View(model);
            }

            var userId = GetCurrentUserId();
            var updated = _accountService.UpdateProfile(userId, model);
            if (!updated)
            {
                ModelState.AddModelError("", "Không thể cập nhật hồ sơ.");
                model.Addresses = _accountService.GetProfile(userId)?.Addresses ?? new List<AddressItemVM>();
                return View(model);
            }

            await RefreshIdentityAsync(userId, model.FullName, model.Email);
            TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ProfileVM model)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(model.CurrentPassword) ||
                string.IsNullOrWhiteSpace(model.NewPassword) ||
                string.IsNullOrWhiteSpace(model.ConfirmNewPassword))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin đổi mật khẩu.";
                return RedirectToAction(nameof(Profile));
            }

            if (model.NewPassword != model.ConfirmNewPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp.";
                return RedirectToAction(nameof(Profile));
            }

            if (model.NewPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return RedirectToAction(nameof(Profile));
            }

            var success = _accountService.ChangePassword(userId, model.CurrentPassword, model.NewPassword);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Đổi mật khẩu thành công."
                : "Mật khẩu hiện tại không đúng.";

            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddAddress(ProfileVM model)
        {
            if (!string.IsNullOrWhiteSpace(model.NewAddressPhoneNumber))
            {
                if (!Regex.IsMatch(model.NewAddressPhoneNumber, @"^(0[3-9])\d{8}$"))
                {
                    TempData["ErrorMessage"] = "Số điện thoại giao hàng không hợp lệ.";
                    return RedirectToAction(nameof(Profile));
                }
            }

            var success = _accountService.AddAddress(GetCurrentUserId(), model);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Thêm địa chỉ thành công."
                : "Không thể thêm địa chỉ. Vui lòng kiểm tra lại thông tin.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAddress(int addressId)
        {
            var success = _accountService.DeleteAddress(GetCurrentUserId(), addressId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Xóa địa chỉ thành công."
                : "Không thể xóa địa chỉ.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetDefaultAddress(int addressId)
        {
            var success = _accountService.SetDefaultAddress(GetCurrentUserId(), addressId);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? "Đã đặt địa chỉ mặc định."
                : "Không thể cập nhật địa chỉ mặc định.";
            return RedirectToAction(nameof(Profile));
        }

        private int GetCurrentUserId()
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : 0;
        }

        private async Task RefreshIdentityAsync(int userId, string fullName, string email)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "Customer";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }
    }
}
