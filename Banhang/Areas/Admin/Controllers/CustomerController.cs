using System;
using Banhang.Data;
using Banhang.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Banhang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class CustomerController : AdminBaseController
    {
        private readonly UserDAO _userDao;
        private readonly OrderDAO _orderDao;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(UserDAO userDao, OrderDAO orderDao, ILogger<CustomerController> logger)
        {
            _userDao = userDao;
            _orderDao = orderDao;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var customers = _userDao.GetAllCustomers();
            return View(customers);
        }

        public IActionResult Details(int id)
        {
            var customer = _userDao.GetUserByID(id);
            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.Equals(customer.RoleName, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                TempData["WarningMessage"] = "Tài khoản không thuộc nhóm khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            var orders = _orderDao.GetOrdersByUser(id);
            var model = new CustomerDetailViewModel
            {
                Customer = customer,
                Orders = orders
            };

            return View(model);
        }
    }
}
