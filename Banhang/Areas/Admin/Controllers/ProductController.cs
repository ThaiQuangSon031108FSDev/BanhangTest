using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Banhang.Data;
using Banhang.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Banhang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Employee")]
    public class ProductController : AdminBaseController
    {
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png"
        };

        private const long MaxImageSizeBytes = 5 * 1024 * 1024;

        private readonly ProductDAO _productDao;
        private readonly CategoryDAO _categoryDao;
        private readonly ILogger<ProductController> _logger;
        private readonly IWebHostEnvironment _environment;

        public ProductController(ProductDAO p, CategoryDAO c, ILogger<ProductController> logger, IWebHostEnvironment environment)
        {
            _productDao = p;
            _categoryDao = c;
            _logger = logger;
            _environment = environment;
        }

        public IActionResult Index(int page = 1)
        {
            page = Math.Max(1, page);
            var paginatedList = _productDao.GetProductsPaginated(page, 10);
            return View(paginatedList);
        }

        [HttpGet]
        public IActionResult Create()
        {
            PopulateCategories();
            return View(new Product { IsActive = true, Stock = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product p, IFormFile? imageFile)
        {
            var uploadSuccess = await ProcessImageUploadAsync(p, imageFile, null);
            if (!ModelState.IsValid || !uploadSuccess)
            {
                PopulateCategories();
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
                PopulateCategories();
                return View(p);
            }
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var p = _productDao.GetProductByID(id);
            if (p == null) return NotFound();
            PopulateCategories();
            return View(p);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product p, IFormFile? imageFile)
        {
            var originalImage = p.ImageUrl;
            var uploadSuccess = await ProcessImageUploadAsync(p, imageFile, originalImage);
            if (!ModelState.IsValid || !uploadSuccess)
            {
                PopulateCategories();
                _logger.LogWarning("ModelState invalid khi cập nhật sản phẩm: {ProductId}", p.ProductID);
                return View(p);
            }

            try
            {
                if (!_productDao.UpdateProduct(p))
                {
                    _logger.LogWarning("Cập nhật sản phẩm thất bại: {ProductId}", p.ProductID);
                    PopulateCategories();
                    ViewBag.Error = "Cập nhật thất bại";
                    return View(p);
                }

                _logger.LogInformation("Đã cập nhật sản phẩm ID: {ProductId}", p.ProductID);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật sản phẩm: {ProductId}", p.ProductID);
                PopulateCategories();
                ViewBag.Error = "Có lỗi xảy ra khi cập nhật sản phẩm.";
                return View(p);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
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

        private async Task<bool> ProcessImageUploadAsync(Product product, IFormFile? imageFile, string? existingImagePath)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return true;
            }

            if (imageFile.Length > MaxImageSizeBytes)
            {
                ModelState.AddModelError(nameof(product.ImageUrl), "Ảnh phải nhỏ hơn 5MB.");
                return false;
            }

            if (!AllowedContentTypes.Contains(imageFile.ContentType))
            {
                ModelState.AddModelError(nameof(product.ImageUrl), "Chỉ chấp nhận ảnh JPEG hoặc PNG.");
                return false;
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(imageFile.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = imageFile.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";
            }
            else
            {
                extension = extension.ToLowerInvariant();
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = System.IO.File.Create(filePath))
            {
                await imageFile.CopyToAsync(stream);
            }

            if (!string.IsNullOrEmpty(existingImagePath))
            {
                TryDeleteImage(existingImagePath);
            }

            product.ImageUrl = $"/images/products/{fileName}";
            return true;
        }

        private void TryDeleteImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !imagePath.StartsWith("/images/products/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var relativePath = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_environment.WebRootPath, relativePath);

            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    System.IO.File.Delete(fullPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể xóa ảnh cũ tại {Path}", fullPath);
                }
            }
        }

        private void PopulateCategories()
        {
            ViewBag.Categories = _categoryDao.GetAllCategories();
        }
    }
}
