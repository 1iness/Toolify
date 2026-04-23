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
            await _productRepo.AddToCartAsync(id, userId, guestId);

            TempData["ToastMessage"] = "Товар добавлен в корзину";
            TempData["ToastType"] = "success";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Remove(int id)
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);
            await _productRepo.RemoveFromCartAsync(id, userId, guestId);

            TempData["ToastMessage"] = "Товар удалён из корзины";
            TempData["ToastType"] = "info";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);
            await _productRepo.ClearCartAsync(userId, guestId);

            TempData["ToastMessage"] = "Корзина очищена";
            TempData["ToastType"] = "info";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ChangeQuantity(int id, int change)
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);
            bool isUpdated = await _productRepo.UpdateQuantityAsync(id, userId, guestId, change);

            if (!isUpdated && change > 0)
            {
                TempData["ToastMessage"] = "Недостаточно товара на складе";
                TempData["ToastType"] = "error";
            }
            else
            {
                TempData["ToastMessage"] = "Количество обновлено";
                TempData["ToastType"] = "success";
            }
                return RedirectToAction("Index");
        }

        [HttpPost] 
        public async Task<IActionResult> AddToCartApi(int id)
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);

            await _productRepo.AddToCartAsync(id, userId, guestId);
            var cartItems = await _productRepo.GetCartItemsAsync(userId, guestId);
            var totalCount = cartItems.Sum(x => x.Quantity);
            var productQuantity = cartItems.FirstOrDefault(x => x.ProductId == id)?.Quantity ?? 0;
            return Json(new { success = true, count = totalCount, productQuantity });
        }

        [HttpPost]
        public async Task<IActionResult> ChangeQuantityApi(int id, int change)
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);
            var isUpdated = await _productRepo.UpdateQuantityAsync(id, userId, guestId, change);
            var cartItems = await _productRepo.GetCartItemsAsync(userId, guestId);
            var totalCount = cartItems.Sum(x => x.Quantity);
            var productQuantity = cartItems.FirstOrDefault(x => x.ProductId == id)?.Quantity ?? 0;

            if (!isUpdated && change > 0)
            {
                return Json(new
                {
                    success = false,
                    count = totalCount,
                    productQuantity,
                    message = "Недостаточно товара на складе"
                });
            }

            return Json(new { success = true, count = totalCount, productQuantity });
        }

        [HttpGet]
        public async Task<IActionResult> GetCartProductQuantities()
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);
            var cartItems = await _productRepo.GetCartItemsAsync(userId, guestId);
            var quantities = cartItems.ToDictionary(x => x.ProductId, x => x.Quantity);
            return Json(quantities);
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
