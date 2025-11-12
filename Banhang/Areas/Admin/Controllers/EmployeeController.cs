using Microsoft.AspNetCore.Mvc;
using Banhang.Data;
using Banhang.Models;

// 1. Namespace phải khớp với vị trí thư mục Area
namespace Banhang.Areas.Admin.Controllers
{
    // 2. Thuộc tính [Area("Admin")] là BẮT BUỘC
    //    Nó báo cho ASP.NET Core biết controller này thuộc Area "Admin"
    [Area("Admin")]
    public class EmployeeController : AdminBaseController
    {
        private readonly UserDAO _userDao;

        // Constructor này đã sửa lỗi CS8618 (cảnh báo null)
        public EmployeeController(UserDAO userDao)
        {
            _userDao = userDao ?? throw new ArgumentNullException(nameof(userDao));
        }

        public IActionResult Index()
        {
            var guard = GuardAdminOnly();
            if (guard is not null) return guard;

            var list = _userDao.GetAllEmployees();
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var guard = GuardAdminOnly();
            if (guard is not null) return guard;

            return View(new User());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(User u, string password, bool isAdmin = false)
        {
            var guard = GuardAdminOnly();
            if (guard is not null) return guard;

            // 3. Kiểm tra ModelState.IsValid là một thói quen tốt
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Dữ liệu không hợp lệ";
                return View(u);
            }

            var id = _userDao.InsertEmployee(u, password, isAdmin);
            if (id <= 0)
            {
                ViewBag.Error = "Tạo nhân viên thất bại.";
                return View(u);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}