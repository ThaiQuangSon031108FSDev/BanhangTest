using Microsoft.AspNetCore.Mvc;
using Banhang.Data;
using Banhang.Models;

namespace Banhang.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProductDAO _productDao;
        private readonly CategoryDAO _categoryDao;

        public HomeController(ProductDAO productDao, CategoryDAO categoryDao)
        {
            _productDao = productDao;
            _categoryDao = categoryDao;
        }

        // Trang chủ: hiển thị danh sách sản phẩm (mới/đề xuất)
        public IActionResult Index(int? categoryId)
        {
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
            if (p == null) return NotFound();
            return View(p);
        }

        // Tìm kiếm theo tên/giá (đơn giản)
        [HttpGet]
        public IActionResult Search(string q)
        {
            q ??= "";
            var results = _productDao.GetProductsByName(q);
            ViewBag.Query = q;
            return View("Search", results);
        }
    }
}
