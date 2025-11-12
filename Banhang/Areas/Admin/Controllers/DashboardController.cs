using Microsoft.AspNetCore.Mvc;
using Banhang.Data;
using Banhang.Models.Admin;

namespace Banhang.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : AdminBaseController
    {
        private readonly ProductDAO _productDao;
        private readonly UserDAO _userDao;

        public DashboardController(ProductDAO p, UserDAO u)
        {
            _productDao = p;
            _userDao = u;
        }

        public IActionResult Index()
        {
            var guard = GuardAdminOrEmployee();   // cho Admin & Employee vào dashboard
            if (guard is not null) return guard;

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
