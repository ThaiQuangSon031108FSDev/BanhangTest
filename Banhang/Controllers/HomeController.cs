using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Banhang.Data;
using Banhang.Models;

namespace Banhang.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProductDAO _productDao;
        private readonly CategoryDAO _categoryDao;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ProductDAO productDao, CategoryDAO categoryDao, ILogger<HomeController> logger)
        {
            _productDao = productDao;
            _categoryDao = categoryDao;
            _logger = logger;
        }

        // Trang chủ: hiển thị danh sách sản phẩm (mới/đề xuất)
        public IActionResult Index(int? categoryId)
        {
            _logger.LogInformation("User {Username} truy cập trang chủ", User.Identity?.Name ?? "anonymous");
            var products = _productDao.GetAllProducts();
            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryID == categoryId.Value).ToList();

            ViewBag.Categories = _categoryDao.GetAllCategories();
            return View(products);
        }

        // Chi tiết sản phẩm
        public IActionResult Details(int id)
        {
            var p = _productDao.GetProductByID(id);
            if (p == null)
            {
                _logger.LogWarning("Không tìm thấy sản phẩm {ProductId}", id);
                return NotFound();
            }

            _logger.LogInformation("Xem chi tiết sản phẩm {ProductId}", id);
            return View(p);
        }

        // Tìm kiếm theo tên/giá (đơn giản)
        [HttpGet]
        public IActionResult Search(string q)
        {
            q ??= string.Empty;
            _logger.LogInformation("Tìm kiếm sản phẩm với từ khóa {Keyword}", q);
            var results = _productDao.GetProductsByName(q);
            ViewBag.Query = q;
            return View("Search", results);
        }

        [Route("Home/Error")]
        public IActionResult Error()
        {
            _logger.LogError("Chuyển đến trang lỗi chung từ HomeController");
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}
