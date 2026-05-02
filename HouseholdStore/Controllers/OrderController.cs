using HouseholdStore.Helpers;
using HouseholdStore.Models;
using HouseholdStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Security.Claims;
using Toolify.AuthService.Services;
using Toolify.ProductService.Data;
using Toolify.ProductService.Models;

namespace HouseholdStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly ProductRepository _productRepo;
        private readonly AuthApiService _authService;
        private readonly EmailService _emailService;
        private readonly ProductApiService _api;

        public OrderController(ProductRepository productRepo, AuthApiService authService, EmailService emailService, ProductApiService api)
        {
            _productRepo = productRepo;
            _authService = authService;
            _emailService = emailService;
            _api = api;
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

            if (!cartItems.Any(c => c.PurchasableQuantity > 0))
            {
                TempData["ToastMessage"] = "Сейчас нет товаров, доступных для заказа — проверьте остатки в корзине.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutViewModel
            {
                CartItems = cartItems
            };

            await FillPreviewAsync(model, userId, guestId, null);

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
        public async Task<IActionResult> Preview(string? promoCode)
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);

            var courier = await _productRepo.PreviewCheckoutTotalsAsync(userId, guestId, promoCode, "Courier");
            var pickup  = await _productRepo.PreviewCheckoutTotalsAsync(userId, guestId, promoCode, "Pickup");

            if (courier == null || pickup == null)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                goods = courier.NetGoodsAmount,
                promoPercent = courier.PromoPercent,
                promoAmount = courier.PromoDiscountAmount,
                appliedFixed = courier.AppliedFixedDiscountAmount,
                deliveryCourier = courier.DeliveryFee,
                deliveryPickup = pickup.DeliveryFee,
                grandCourier = courier.GrandTotal,
                grandPickup = pickup.GrandTotal,
                rules = courier.AppliedRules.Select(r => new { r.Kind, r.Title, r.Amount })
            });
        }

        private async Task FillPreviewAsync(CheckoutViewModel model, int? userId, string? guestId, string? promoCode)
        {
            var courier = await _productRepo.PreviewCheckoutTotalsAsync(userId, guestId, promoCode, "Courier");
            var pickup  = await _productRepo.PreviewCheckoutTotalsAsync(userId, guestId, promoCode, "Pickup");

            if (courier == null || pickup == null) return;

            model.PreviewGoods            = courier.NetGoodsAmount;
            model.PreviewPromoPercent     = courier.PromoPercent;
            model.PreviewPromoAmount      = courier.PromoDiscountAmount;
            model.PreviewAppliedFixed     = courier.AppliedFixedDiscountAmount;
            model.PreviewDeliveryCourier  = courier.DeliveryFee;
            model.PreviewDeliveryPickup   = pickup.DeliveryFee;
            model.PreviewGrandCourier     = courier.GrandTotal;
            model.PreviewGrandPickup      = pickup.GrandTotal;
            model.PreviewAppliedRules     = courier.AppliedRules;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CheckoutViewModel model)
        {
            var (userId, guestId) = CartHelper.GetCartIdentifiers(HttpContext);

            model.DeliveryType = model.DeliveryType?.Trim() ?? "Courier";
            model.PaymentMethod = model.PaymentMethod?.Trim() ?? "CardOnDelivery";

            if (model.DeliveryType != "Courier" && model.DeliveryType != "Pickup")
                ModelState.AddModelError(nameof(model.DeliveryType), "Некорректный способ доставки");
            if (model.PaymentMethod != "CardOnDelivery" && model.PaymentMethod != "CashOnDelivery")
                ModelState.AddModelError(nameof(model.PaymentMethod), "Некорректный способ оплаты");

            if (model.DeliveryType == "Pickup")
            {
                model.Address = "Самовывоз";
                ModelState.Remove(nameof(model.Address));
            }


            if (!ModelState.IsValid)
            {
                model.CartItems = await _productRepo.GetCartItemsAsync(userId, guestId);
                return View("Checkout", model);
            }

            var cartBeforePay = await _productRepo.GetCartItemsAsync(userId, guestId);
            if (!cartBeforePay.Any(c => c.PurchasableQuantity > 0))
            {
                TempData["ToastMessage"] = "Сейчас нет товаров, доступных для заказа — проверьте остатки в корзине.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Cart");
            }

            var order = new Order
            {
                UserId = userId,
                GuestFirstName = model.FirstName,
                GuestLastName = model.LastName,
                GuestEmail = model.Email,
                GuestPhone = model.Phone,
                Address = model.Address,
                PromoCode = model.PromoCode,
                DeliveryType = model.DeliveryType,
                PaymentMethod = model.PaymentMethod

            };

            try
            {
                int orderId = await _productRepo.CreateOrderAsync(order, guestId, model.PromoCode);
                return RedirectToAction("Confirmed", new { id = orderId });
            }
            catch (SqlException ex) when (ex.Number == 50001 || ex.Number == 50002)
            {
                TempData["ToastMessage"] = ex.Number == 50002
                    ? "Корзина пуста. Оформление отменено."
                    : "Нет товаров, доступных для заказа. Проверьте наличие.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Cart");
            }
        }

        public async Task<IActionResult> Confirmed(int id)
        {
            string? email = null;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                email = User.Identity.Name;
            }

            if (string.IsNullOrEmpty(email))
            {
                email = await _productRepo.GetOrderEmailAsync(id);
            }

            if (!string.IsNullOrEmpty(email))
            {
                var details = await _productRepo.GetOrderEmailDetailsAsync(id);
                if (details != null)
                {
                    var html = OrderConfirmationEmailHtmlBuilder.Build(details);
                    await _emailService.SendOrderConfirmedHtmlAsync(email, id, html);
                }
                else
                    await _emailService.SendOrderConfirmedAsync(email, id);
            }

            return View(id);
        }

        [HttpPost]
        public async Task<IActionResult> ApplyPromo(string code, decimal? goodsTotal = null)
        {
            var discount = await _api.GetPromoDiscountAsync(code, goodsTotal);
            if (discount.HasValue)
            {
                return Json(new { success = true, discount = discount.Value });
            }
            return Json(new { success = false, message = "Неверный промокод" });
        }

    }
}
