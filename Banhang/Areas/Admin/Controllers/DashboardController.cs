using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Banhang.Data;
using Banhang.Models.Admin;

namespace Banhang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class DashboardController : AdminBaseController
    {
        private readonly ProductDAO _productDao;
        private readonly UserDAO _userDao;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ProductDAO p, UserDAO u, ILogger<DashboardController> logger)
        {
            _productDao = p;
            _userDao = u;
            _logger = logger;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("User {Username} truy cập dashboard", User.Identity?.Name);

            var vm = new DashboardVM
            {
                TotalProducts = _productDao.CountProducts(),
                TotalUsers = _userDao.CountUsers(),
                TotalEmployees = _userDao.CountEmployees(),
                TotalCustomers = _userDao.CountCustomers(),
                TotalRevenue = _productDao.GetTotalRevenue()
            };

            return View(vm);
        }
    }
}
