using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Banhang.Data;
using Banhang.Extensions;
using Banhang.Models;
using Banhang.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Banhang.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly OrderDAO _orderDao;
        private readonly UserDAO _userDao;
        private readonly ProductDAO _productDao;
        private readonly ILogger<OrderController> _logger;
        private const string CartSessionKey = "CART_SESSION";

        public OrderController(OrderDAO orderDao, UserDAO userDao, ProductDAO productDao, ILogger<OrderController> logger)
        {
            _orderDao = orderDao;
            _userDao = userDao;
            _productDao = productDao;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            if (!TryValidateCart(out var cart, out var redirectResult))
            {
                return redirectResult ?? RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutViewModel
            {
                CartItems = cart,
                TotalAmount = cart.Sum(i => i.Subtotal)
            };

            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                var user = _userDao.GetUserByID(userId);
                if (user != null)
                {
                    if (!string.IsNullOrWhiteSpace(user.FullName))
                    {
                        model.ShipName = user.FullName;
                    }

                    if (!string.IsNullOrWhiteSpace(user.Phone))
                    {
                        model.ShipPhone = user.Phone;
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlaceOrder(CheckoutViewModel model)
        {
            if (!TryValidateCart(out var cart, out var redirectResult))
            {
                return redirectResult ?? RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                model.CartItems = cart;
                model.TotalAmount = cart.Sum(i => i.Subtotal);
                return View("Checkout", model);
            }

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                _logger.LogWarning("Không thể xác định user ID khi đặt hàng");
                return Forbid();
            }

            var order = new Order
            {
                UserID = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = cart.Sum(i => i.Subtotal),
                Status = "Pending",
                ShipName = model.ShipName,
                ShipAddress = model.ShipAddress,
                ShipPhone = model.ShipPhone,
                Notes = model.Notes
            };

            try
            {
                var orderId = _orderDao.CreateOrder(order, cart);
                HttpContext.Session.Remove(CartSessionKey);
                TempData["SuccessMessage"] = "Đặt hàng thành công!";
                _logger.LogInformation("User {UserId} đã đặt đơn hàng {OrderId}", userId, orderId);
                return RedirectToAction("Confirmation", new { id = orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo đơn hàng cho user {UserId}", userId);
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi tạo đơn hàng. Vui lòng thử lại.");
                model.CartItems = cart;
                model.TotalAmount = cart.Sum(i => i.Subtotal);
                return View("Checkout", model);
            }
        }

        [HttpGet]
        public IActionResult Confirmation(int id)
        {
            var order = _orderDao.GetOrderWithDetails(id);
            if (order == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && !User.IsInRole("Employee"))
            {
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || order.UserID != userId)
                {
                    return Forbid();
                }
            }

            return View(order);
        }

        private bool TryValidateCart(out List<CartItem> cart, out IActionResult? redirectResult)
        {
            cart = GetCart();

            if (cart.Count == 0)
            {
                TempData["WarningMessage"] = "Giỏ hàng của bạn đang trống.";
                redirectResult = RedirectToAction("Index", "Cart");
                return false;
            }

            var removedProducts = new List<string>();
            var adjustedQuantities = new List<string>();
            var priceUpdated = new List<string>();

            foreach (var item in cart.ToArray())
            {
                var product = _productDao.GetProductByID(item.ProductID);
                if (product == null || !product.IsActive || product.Stock <= 0)
                {
                    removedProducts.Add(item.ProductName);
                    _logger.LogWarning("Loại bỏ sản phẩm {ProductId} khỏi giỏ hàng khi checkout vì không khả dụng", item.ProductID);
                    cart.Remove(item);
                    continue;
                }

                if (item.Quantity > product.Stock)
                {
                    item.Quantity = product.Stock;
                    adjustedQuantities.Add(product.ProductName);
                    _logger.LogWarning("Điều chỉnh số lượng sản phẩm {ProductId} còn {Quantity} do giới hạn tồn kho", item.ProductID, item.Quantity);
                }

                if (item.Price != product.Price)
                {
                    priceUpdated.Add(product.ProductName);
                    _logger.LogInformation("Cập nhật giá sản phẩm {ProductId} khi checkout từ {OldPrice} lên {NewPrice}", item.ProductID, item.Price, product.Price);
                }

                item.Price = product.Price;
                item.ProductName = product.ProductName;
                item.ImageUrl = product.ImageUrl;
            }

            SaveCart(cart);

            if (cart.Count == 0)
            {
                var reason = removedProducts.Count > 0
                    ? $"Một số sản phẩm không còn khả dụng: {string.Join(", ", removedProducts)}."
                    : "Giỏ hàng của bạn đang trống.";
                TempData["WarningMessage"] = reason;
                redirectResult = RedirectToAction("Index", "Cart");
                return false;
            }

            if (removedProducts.Count > 0 || adjustedQuantities.Count > 0 || priceUpdated.Count > 0)
            {
                var messages = new List<string>();
                if (removedProducts.Count > 0)
                {
                    messages.Add($"Các sản phẩm không còn khả dụng đã được xóa: {string.Join(", ", removedProducts)}.");
                }

                if (adjustedQuantities.Count > 0)
                {
                    messages.Add($"Đã điều chỉnh số lượng theo tồn kho hiện tại cho: {string.Join(", ", adjustedQuantities)}.");
                }

                if (priceUpdated.Count > 0)
                {
                    messages.Add($"Giá bán đã được cập nhật cho: {string.Join(", ", priceUpdated)}.");
                }

                TempData["WarningMessage"] = string.Join(" ", messages);
                redirectResult = RedirectToAction("Index", "Cart");
                return false;
            }

            redirectResult = null;
            return true;
        }

        private List<CartItem> GetCart()
        {
            return HttpContext.Session.GetObject<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> items)
        {
            HttpContext.Session.SetObject(CartSessionKey, items);
        }
    }
}
