using System;
using System.Collections.Generic;
using System.Linq;
using Banhang.Data;
using Banhang.Extensions;
using Banhang.Models;
using Microsoft.AspNetCore.Mvc;

namespace Banhang.Controllers
{
    public class CartController : Controller
    {
        private readonly ProductDAO _productDao;
        private const string CART_KEY = "CART_SESSION";

        public CartController(ProductDAO productDao)
        {
            _productDao = productDao;
        }

        private List<CartItem> GetCart()
            => HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();

        private void SaveCart(List<CartItem> items)
            => HttpContext.Session.SetObject(CART_KEY, items);

        [HttpPost]
        public IActionResult Add(int productId, int quantity = 1)
        {
            var p = _productDao.GetProductByID(productId);
            if (p == null) return NotFound();

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
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCart();
            ViewBag.Total = cart.Sum(i => i.Subtotal);
            return View(cart);
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            var cart = GetCart();
            cart.RemoveAll(x => x.ProductID == productId);
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Update(int productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductID == productId);
            if (item != null)
            {
                item.Quantity = Math.Max(1, quantity);
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }
    }
}
