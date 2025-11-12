using System.Linq;
using Banhang.Data;
using Banhang.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banhang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class ReportController : AdminBaseController
    {
        private readonly OrderDAO _orderDao;

        public ReportController(OrderDAO orderDao)
        {
            _orderDao = orderDao;
        }

        public IActionResult Index()
        {
            var revenue = _orderDao.GetMonthlyRevenue(6)
                .Select(r => new MonthlyRevenuePoint { Month = r.Month, Total = r.Total })
                .ToList();

            var topProducts = _orderDao.GetTopProducts(5)
                .Select(p => new TopProductReportItem
                {
                    ProductName = p.ProductName,
                    Quantity = p.Quantity,
                    Revenue = p.Revenue
                })
                .ToList();

            var model = new ReportDashboardViewModel
            {
                MonthlyRevenue = revenue,
                TopProducts = topProducts
            };

            return View(model);
        }
    }
}
