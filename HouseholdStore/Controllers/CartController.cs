using HouseholdStore.Extensions;
using HouseholdStore.Models;
using Microsoft.AspNetCore.Mvc;
using HouseholdStore.Helpers;
using Toolify.ProductService.Data;

namespace HouseholdStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ProductRepository _productRepo;

        public CartController(ProductRepository productRepo)
        {
            _productRepo = productRepo;
        }

        public async Task<IActionResult> Index()
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);
            var cartItems = await _productRepo.GetCartItemsAsync(userId, guestId);
            return View(cartItems);
        }

        public async Task<IActionResult> AddToCart(int id)
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);
            _productRepo.AddToCart(id, userId, guestId);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Remove(int id)
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);
            await _productRepo.RemoveFromCartAsync(id, userId, guestId);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ChangeQuantity(int id, int change)
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);
            await _productRepo.UpdateQuantityAsync(id, userId, guestId, change);
            return RedirectToAction("Index");
        }

        [HttpPost] 
        public async Task<IActionResult> AddToCartApi(int id)
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);

            _productRepo.AddToCart(id, userId, guestId);
            var cartItems = await _productRepo.GetCartItemsAsync(userId, guestId);
            var totalCount = cartItems.Sum(x => x.Quantity);
            return Json(new { success = true, count = totalCount });
        }
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);
            var cartItems = await _productRepo.GetCartItemsAsync(userId, guestId);
            return Json(new { count = cartItems.Sum(x => x.Quantity) });
        }
    }
}
