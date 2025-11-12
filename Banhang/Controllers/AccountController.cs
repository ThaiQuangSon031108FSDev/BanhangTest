using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Banhang.Common;
using Banhang.Data;
using Banhang.Models;
using Banhang.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Banhang.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserDAO _userDao;
        private readonly ILogger<AccountController> _logger;
        private readonly OrderDAO _orderDao;

        public AccountController(UserDAO userDao, OrderDAO orderDao, ILogger<AccountController> logger)
        {
            _userDao = userDao;
            _orderDao = orderDao;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null, bool remember = false)
        {
            ViewBag.ReturnUrl = returnUrl;
            var user = _userDao.CheckLogin(username, password, out var passwordUpgraded);
            if (user == null)
            {
                _logger.LogWarning("Đăng nhập thất bại cho username {Username}", username);
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu.";
                return View();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.RoleName ?? string.Empty)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = remember,
                ExpiresUtc = remember ? DateTimeOffset.UtcNow.AddDays(7) : (DateTimeOffset?)null
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (remember)
            {
                Response.Cookies.Append("remember_username", user.Username, new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(7),
                    HttpOnly = true,
                    Secure = true,
                    IsEssential = true
                });
            }
            else if (Request.Cookies.ContainsKey("remember_username"))
            {
                Response.Cookies.Delete("remember_username");
            }

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetInt32("RoleID", user.RoleID);
            HttpContext.Session.SetString("RoleName", user.RoleName ?? string.Empty);

            if (passwordUpgraded)
            {
                _logger.LogInformation("Đã nâng cấp password hash cho người dùng {Username}", username);
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User input, string password)
        {
            input.RoleID = Roles.Customer;
            var newId = _userDao.RegisterUser(input, password);
            if (newId <= 0)
            {
                _logger.LogWarning("Đăng ký thất bại cho username {Username}", input.Username);
                ViewBag.Error = "Đăng ký thất bại.";
                return View(input);
            }

            _logger.LogInformation("Người dùng {Username} đã đăng ký thành công với ID {UserId}", input.Username, newId);
            return RedirectToAction("Login");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = _userDao.GetUserByEmail(model.Email);
                if (user != null)
                {
                    var token = _userDao.CreatePasswordResetToken(user.UserID, TimeSpan.FromHours(1));
                    var callbackUrl = Url.Action(nameof(ResetPassword), "Account", new { token }, Request.Scheme);
                    _logger.LogInformation("Password reset link for {Email}: {Link}", model.Email, callbackUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi yêu cầu reset password cho email {Email}", model.Email);
            }

            TempData["SuccessMessage"] = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi hướng dẫn reset mật khẩu.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["Error"] = "Token không hợp lệ.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var user = _userDao.GetUserByResetToken(token);
            if (user == null)
            {
                TempData["Error"] = "Token đã hết hạn hoặc không tồn tại.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = _userDao.ResetPasswordWithToken(model.Token, model.NewPassword);
                if (!success)
                {
                    ModelState.AddModelError(string.Empty, "Token đã hết hạn hoặc không hợp lệ.");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi reset password bằng token");
                ModelState.AddModelError(string.Empty, "Không thể đặt lại mật khẩu. Vui lòng thử lại.");
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            if (Request.Cookies.ContainsKey("remember_username"))
            {
                Response.Cookies.Delete("remember_username");
            }
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost]
        public IActionResult KeepAlive()
        {
            HttpContext.Session.SetString("KeepAlive", DateTime.Now.ToString());
            return Ok();
        }

        [Authorize]
        public IActionResult Profile()
        {
            if (!TryGetCurrentUser(out var user))
            {
                return RedirectToAction("Login");
            }

            var model = new ProfileViewModel
            {
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleName = user.RoleName ?? string.Empty,
                CreatedAt = user.CreatedAt,
                RecentOrders = _orderDao.GetOrdersByUser(user.UserID)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public IActionResult EditProfile()
        {
            if (!TryGetCurrentUser(out var user))
            {
                return RedirectToAction("Login");
            }

            var model = new EditProfileViewModel
            {
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(EditProfileViewModel model)
        {
            if (!TryGetCurrentUser(out var user))
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Phone = model.Phone;

            try
            {
                var updated = _userDao.UpdateUserProfile(user);
                if (updated)
                {
                    TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công.";
                    _logger.LogInformation("Người dùng {UserId} đã cập nhật hồ sơ", user.UserID);
                }
                else
                {
                    TempData["WarningMessage"] = "Không có thay đổi nào được lưu.";
                }
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật hồ sơ cho người dùng {UserId}", user.UserID);
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật hồ sơ.";
                return View(model);
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!TryGetCurrentUser(out var user))
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var changed = _userDao.ChangePassword(user.UserID, model.CurrentPassword, model.NewPassword);
                if (!changed)
                {
                    ModelState.AddModelError(string.Empty, "Mật khẩu hiện tại không chính xác.");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
                _logger.LogInformation("Người dùng {UserId} đã đổi mật khẩu", user.UserID);
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đổi mật khẩu cho người dùng {UserId}", user.UserID);
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi đổi mật khẩu. Vui lòng thử lại.");
                return View(model);
            }
        }

        private bool TryGetCurrentUser(out User? user)
        {
            user = null;
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return false;
            }

            user = _userDao.GetUserByID(userId);
            return user != null;
        }
    }
}
