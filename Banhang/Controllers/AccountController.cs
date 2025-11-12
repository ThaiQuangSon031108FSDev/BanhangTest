using System;
using Banhang.Data;
using Banhang.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Banhang.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserDAO _userDao;
        public AccountController(UserDAO userDao) { _userDao = userDao; }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password, string? returnUrl = null, bool remember = false)
        {
            var user = _userDao.CheckLogin(username, password);
            if (user == null)
            {
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu.";
                return View();
            }

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetInt32("RoleID", user.RoleID);
            HttpContext.Session.SetString("RoleName", user.RoleName ?? "");

            if (remember)
                Response.Cookies.Append("remember_username", user.Username, new CookieOptions { Expires = DateTimeOffset.Now.AddDays(7) });
            else if (Request.Cookies.ContainsKey("remember_username"))
                Response.Cookies.Delete("remember_username");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User input, string password)
        {
            input.RoleID = 3;
            var newId = _userDao.RegisterUser(input, password);
            if (newId <= 0)
            {
                ViewBag.Error = "Đăng ký thất bại.";
                return View(input);
            }
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult KeepAlive()
        {
            HttpContext.Session.SetString("KeepAlive", DateTime.Now.ToString());
            return Ok();
        }
    }
}
