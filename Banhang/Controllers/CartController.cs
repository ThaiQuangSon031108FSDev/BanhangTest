using System;
using System.Collections.Generic;
using System.Linq;
using Banhang.Data;
using Banhang.Extensions;
using Banhang.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Banhang.Controllers
{
    public class CartController : Controller
    {
        private readonly ProductDAO _productDao;
        private readonly ILogger<CartController> _logger;
        private const string CART_KEY = "CART_SESSION";

        public CartController(ProductDAO productDao, ILogger<CartController> logger)
        {
            _productDao = productDao;
            _logger = logger;
        }

        private List<CartItem> GetCart()
            => HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();

        private void SaveCart(List<CartItem> items)
            => HttpContext.Session.SetObject(CART_KEY, items);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int productId, int quantity = 1)
        {
            try
            {
                var p = _productDao.GetProductByID(productId);
                if (p == null)
                {
                    _logger.LogWarning("Không tìm thấy sản phẩm {ProductId} để thêm vào giỏ", productId);
                    return NotFound();
                }

                var cart = GetCart();
                var item = cart.FirstOrDefault(x => x.ProductID == productId);
                if (item == null)
                {
                    cart.Add(new CartItem
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        ImageUrl = p.ImageUrl,
                        Quantity = quantity
                    });
                }
                else
                {
                    item.Quantity += quantity;
                }

                SaveCart(cart);
                _logger.LogInformation("Đã thêm sản phẩm {ProductId} vào giỏ", productId);
                TempData["SuccessMessage"] = $"Đã thêm {p.ProductName} vào giỏ hàng.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm sản phẩm {ProductId} vào giỏ", productId);
                return StatusCode(500, "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng.");
            }
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCart();
            ViewBag.Total = cart.Sum(i => i.Subtotal);
            _logger.LogInformation("Hiển thị giỏ hàng với {ItemCount} sản phẩm", cart.Count);
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            var cart = GetCart();
            cart.RemoveAll(x => x.ProductID == productId);
            SaveCart(cart);
            _logger.LogInformation("Đã xóa sản phẩm {ProductId} khỏi giỏ", productId);
            TempData["WarningMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductID == productId);
            if (item != null)
            {
                item.Quantity = Math.Max(1, quantity);
                _logger.LogInformation("Cập nhật số lượng sản phẩm {ProductId} thành {Quantity}", productId, item.Quantity);
            }

            SaveCart(cart);
            TempData["SuccessMessage"] = "Đã cập nhật giỏ hàng.";
            return RedirectToAction("Index");
        }
    }
}
