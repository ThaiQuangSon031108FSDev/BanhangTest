using System;
using System.Linq;
using Banhang.Data;
using Banhang.Models;
using Banhang.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Banhang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class OrderController : AdminBaseController
    {
        private static readonly string[] AllowedStatuses = new[]
        {
            "Pending",
            "Processing",
            "Shipped",
            "Delivered",
            "Cancelled"
        };

        private readonly OrderDAO _orderDao;
        private readonly ILogger<OrderController> _logger;

        public OrderController(OrderDAO orderDao, ILogger<OrderController> logger)
        {
            _orderDao = orderDao;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var orders = _orderDao.GetAllOrders();
            var statusGroups = orders
                .GroupBy(o => o.Status)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var model = new AdminOrderListViewModel
            {
                Orders = orders,
                StatusCounts = statusGroups,
                TotalRevenue = orders.Sum(o => o.TotalAmount)
            };

            return View(model);
        }

        public IActionResult Details(int id)
        {
            var order = _orderDao.GetOrderWithDetails(id);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Statuses = AllowedStatuses;
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int id, string status)
        {
            if (!AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Trạng thái không hợp lệ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var normalizedStatus = AllowedStatuses.First(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));

            try
            {
                var success = _orderDao.UpdateStatus(id, normalizedStatus);
                if (!success)
                {
                    TempData["Error"] = "Không thể cập nhật trạng thái đơn hàng.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng #{id} thành {normalizedStatus}.";
                    _logger.LogInformation("{User} cập nhật trạng thái đơn hàng {OrderId} thành {Status}", User.Identity?.Name, id, normalizedStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái đơn hàng {OrderId}", id);
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật trạng thái.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
