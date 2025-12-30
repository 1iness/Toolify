using HouseholdStore.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Toolify.ProductService.Data;

namespace HouseholdStore.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ProductRepository _productRepo;

        public OrderController(ProductRepository productRepo)
        {
            _productRepo = productRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);

            var cartItems = await _productRepo.GetCartItemsAsync(userId, guestId);

            if (cartItems.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string address)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("id")?.Value;

            if (int.TryParse(userIdString, out int userId))
            {
                if (string.IsNullOrWhiteSpace(address))
                {
                    ModelState.AddModelError("", "Введите адрес доставки");
                    return RedirectToAction("Checkout");
                }

                int orderId = await _productRepo.CreateOrderAsync(userId, address);

                return RedirectToAction("Confirmed", new { id = orderId });
            }

            return RedirectToAction("Login", "Account");
        }

        public IActionResult Confirmed(int id)
        {
            return View(id); 
        }
    }
}
