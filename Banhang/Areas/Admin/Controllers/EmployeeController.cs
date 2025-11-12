using System;
using Banhang.Data;
using Banhang.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Banhang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EmployeeController : AdminBaseController
    {
        private readonly UserDAO _userDao;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(UserDAO userDao, ILogger<EmployeeController> logger)
        {
            _userDao = userDao ?? throw new ArgumentNullException(nameof(userDao));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult Index()
        {
            var list = _userDao.GetAllEmployees();
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new User());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(User u, string password, bool isAdmin = false)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Dữ liệu không hợp lệ";
                return View(u);
            }

            try
            {
                var id = _userDao.InsertEmployee(u, password, isAdmin);
                if (id <= 0)
                {
                    ViewBag.Error = "Tạo nhân viên thất bại.";
                    _logger.LogWarning("Không thể tạo nhân viên mới cho username {Username}", u.Username);
                    return View(u);
                }

                _logger.LogInformation("Đã tạo nhân viên mới {Username} với ID {EmployeeId}", u.Username, id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo nhân viên {Username}", u.Username);
                ViewBag.Error = "Có lỗi xảy ra khi tạo nhân viên.";
                return View(u);
            }
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var user = _userDao.GetUserByID(id);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.IsAdmin = string.Equals(user.RoleName, "Admin", StringComparison.OrdinalIgnoreCase);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(User u, bool isAdmin = false)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IsAdmin = isAdmin;
                ViewBag.Error = "Dữ liệu không hợp lệ.";
                return View(u);
            }

            try
            {
                if (_userDao.UpdateEmployee(u, isAdmin))
                {
                    TempData["SuccessMessage"] = "Đã cập nhật thông tin nhân viên.";
                }
                else
                {
                    TempData["WarningMessage"] = "Không có thay đổi nào được áp dụng.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật nhân viên {UserId}", u.UserID);
                ViewBag.Error = "Không thể cập nhật nhân viên.";
                ViewBag.IsAdmin = isAdmin;
                return View(u);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Deactivate(int id)
        {
            try
            {
                if (_userDao.SetUserActiveState(id, false))
                {
                    TempData["SuccessMessage"] = "Đã vô hiệu hóa tài khoản nhân viên.";
                }
                else
                {
                    TempData["WarningMessage"] = "Không tìm thấy nhân viên.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi vô hiệu hóa nhân viên {UserId}", id);
                TempData["Error"] = "Không thể vô hiệu hóa nhân viên.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
