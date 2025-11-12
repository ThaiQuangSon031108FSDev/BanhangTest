using System;
using Banhang.Data;
using Banhang.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Banhang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class CategoryController : AdminBaseController
    {
        private readonly CategoryDAO _categoryDao;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(CategoryDAO categoryDao, ILogger<CategoryController> logger)
        {
            _categoryDao = categoryDao;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var categories = _categoryDao.GetAllCategories();
            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            try
            {
                var id = _categoryDao.CreateCategory(category);
                TempData["SuccessMessage"] = $"Đã tạo danh mục #{id}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo danh mục {CategoryName}", category.CategoryName);
                TempData["Error"] = "Không thể tạo danh mục. Vui lòng thử lại.";
                return View(category);
            }
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var category = _categoryDao.GetCategoryById(id);
            if (category == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục.";
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            try
            {
                if (_categoryDao.UpdateCategory(category))
                {
                    TempData["SuccessMessage"] = "Đã cập nhật danh mục.";
                }
                else
                {
                    TempData["WarningMessage"] = "Không có thay đổi nào được áp dụng.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật danh mục {CategoryId}", category.CategoryID);
                TempData["Error"] = "Không thể cập nhật danh mục.";
                return View(category);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                if (_categoryDao.DeleteCategory(id))
                {
                    TempData["SuccessMessage"] = "Đã xóa danh mục.";
                }
                else
                {
                    TempData["WarningMessage"] = "Danh mục không tồn tại.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa danh mục {CategoryId}", id);
                TempData["Error"] = "Không thể xóa danh mục.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
