using System;
using Banhang.Data;
using Banhang.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Banhang.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : AdminBaseController
    {
        private readonly ProductDAO _productDao;
        private readonly CategoryDAO _categoryDao;
        private readonly ILogger<ProductController> _logger;

        public ProductController(ProductDAO p, CategoryDAO c, ILogger<ProductController> logger)
        {
            _productDao = p;
            _categoryDao = c;
            _logger = logger;
        }

        public IActionResult Index(int page = 1)
        {
            var guard = GuardAdminOrEmployee();
            if (guard != null) return guard;

            page = Math.Max(1, page);
            var paginatedList = _productDao.GetProductsPaginated(page, 10);
            return View(paginatedList);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var guard = GuardAdminOrEmployee();
            if (guard != null) return guard;

            ViewBag.Categories = _categoryDao.GetAllCategories();
            return View(new Product { IsActive = true, Stock = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Product p)
        {
            var guard = GuardAdminOrEmployee();
            if (guard != null) return guard;

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _categoryDao.GetAllCategories();
                _logger.LogWarning("ModelState invalid khi tạo sản phẩm: {ProductName}", p.ProductName);
                return View(p);
            }

            try
            {
                var id = _productDao.InsertProduct(p);
                _logger.LogInformation("Đã tạo sản phẩm mới ID: {ProductId}, Tên: {ProductName}", id, p.ProductName);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo sản phẩm: {ProductName}", p.ProductName);
                ViewBag.Error = "Có lỗi xảy ra khi lưu sản phẩm.";
                ViewBag.Categories = _categoryDao.GetAllCategories();
                return View(p);
            }
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var guard = GuardAdminOrEmployee();
            if (guard != null) return guard;

            var p = _productDao.GetProductByID(id);
            if (p == null) return NotFound();
            ViewBag.Categories = _categoryDao.GetAllCategories();
            return View(p);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product p)
        {
            var guard = GuardAdminOrEmployee();
            if (guard != null) return guard;

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _categoryDao.GetAllCategories();
                _logger.LogWarning("ModelState invalid khi cập nhật sản phẩm: {ProductId}", p.ProductID);
                return View(p);
            }

            try
            {
                if (!_productDao.UpdateProduct(p))
                {
                    _logger.LogWarning("Cập nhật sản phẩm thất bại: {ProductId}", p.ProductID);
                    ViewBag.Categories = _categoryDao.GetAllCategories();
                    ViewBag.Error = "Cập nhật thất bại";
                    return View(p);
                }

                _logger.LogInformation("Đã cập nhật sản phẩm ID: {ProductId}", p.ProductID);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật sản phẩm: {ProductId}", p.ProductID);
                ViewBag.Categories = _categoryDao.GetAllCategories();
                ViewBag.Error = "Có lỗi xảy ra khi cập nhật sản phẩm.";
                return View(p);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var guard = GuardAdminOrEmployee();
            if (guard != null) return guard;

            try
            {
                if (_productDao.DeleteProduct(id))
                {
                    _logger.LogInformation("Đã xóa sản phẩm ID: {ProductId}", id);
                }
                else
                {
                    _logger.LogWarning("Không tìm thấy sản phẩm để xóa: {ProductId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa sản phẩm: {ProductId}", id);
                TempData["Error"] = "Không thể xóa sản phẩm.";
            }

            return RedirectToAction("Index");
        }
    }
}
