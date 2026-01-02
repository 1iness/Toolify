using HouseholdStore.Helpers;
using HouseholdStore.Models;
using HouseholdStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Toolify.ProductService.Data;
using Toolify.ProductService.Models;

namespace HouseholdStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly ProductRepository _productRepo;
        private readonly AuthApiService _authService;
        public OrderController(ProductRepository productRepo, AuthApiService authService)
        {
            _productRepo = productRepo;
            _authService = authService;
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

            var model = new CheckoutViewModel
            {
                CartItems = cartItems
            };

            if (userId.HasValue && User.Identity.IsAuthenticated)
            {
                var email = User.FindFirst(ClaimTypes.Name)?.Value;
                if (!string.IsNullOrEmpty(email))
                {
                    var user = await _authService.GetUserByEmailAsync(email);
                    if (user != null)
                    {
                        model.FirstName = user.FirstName;
                        model.LastName = user.LastName;
                        model.Email = user.Email;
                        model.Phone = user.Phone; 
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CheckoutViewModel model)
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);

            if (!ModelState.IsValid)
            {
                model.CartItems = await _productRepo.GetCartItemsAsync(userId, guestId);
                return View("Checkout", model);
            }

            var order = new Order
            {
                UserId = userId, 
                GuestFirstName = model.FirstName,
                GuestLastName = model.LastName,
                GuestEmail = model.Email,
                GuestPhone = model.Phone,
                Address = model.Address
            };

            int orderId = await _productRepo.CreateOrderAsync(order, guestId);

            return RedirectToAction("Confirmed", new { id = orderId });
        }

        public IActionResult Confirmed(int id)
        {
            return View(id); 
        }
    }
}
