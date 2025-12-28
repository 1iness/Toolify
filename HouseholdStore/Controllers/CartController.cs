using HouseholdStore.Extensions;
using HouseholdStore.Models;
using Microsoft.AspNetCore.Mvc;

namespace HouseholdStore.Controllers
{
    public class CartController : Controller
    {
        private const string CART_KEY = "MyCart";

        public IActionResult Index()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            return View(cart);
        }

        public IActionResult AddToCart(int id, string name, decimal price, string img, decimal? oldPrice)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>(CART_KEY) ?? new List<CartItem>();

            var existingItem = cart.FirstOrDefault(x => x.ProductId == id);
            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = id,
                    ProductName = name,
                    Price = price,
                    OldPrice = oldPrice,
                    ImageUrl = img,
                    Quantity = 1
                });
            }

            HttpContext.Session.Set(CART_KEY, cart);

            return RedirectToAction("Index"); 
        }

        public IActionResult Remove(int id)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(x => x.ProductId == id);
            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.Set(CART_KEY, cart);
            }
            return RedirectToAction("Index");
        }

        public IActionResult ChangeQuantity(int id, int change)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                item.Quantity += change;
                if (item.Quantity < 1) item.Quantity = 1; 
                HttpContext.Session.Set(CART_KEY, cart);
            }
            return RedirectToAction("Index");
        }
    }
}
