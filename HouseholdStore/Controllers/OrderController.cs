using HouseholdStore.Helpers;
using HouseholdStore.Models;
using HouseholdStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Security.Claims;
using Toolify.AuthService.Services;
using Toolify.ProductService.Helpers;
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
            var (courier, pickup, promoRejectReason) = await BuildCheckoutPreviewAsync(userId, guestId, promoCode);

            if (courier == null || pickup == null)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                goods = courier.NetGoodsAmount,
                goodsBeforePromo = courier.GoodsTotalBeforePromo,
                promoPercent = courier.PromoPercent,
                promoAmount = courier.PromoDiscountAmount,
                appliedFixed = courier.AppliedFixedDiscountAmount,
                deliveryCourier = courier.DeliveryFee,
                deliveryPickup = pickup.DeliveryFee,
                grandCourier = courier.GrandTotal,
                grandPickup = pickup.GrandTotal,
                promoRejectReason,
                rules = courier.AppliedRules.Select(r => new { r.Kind, r.Title, r.Amount })
            });
        }

        private async Task<(CheckoutPreviewResult? courier, CheckoutPreviewResult? pickup, string? promoRejectReason)>
            BuildCheckoutPreviewAsync(int? userId, string? guestId, string? promoCode)
        {
            var trimmedPromo = string.IsNullOrWhiteSpace(promoCode) ? null : promoCode.Trim();

            if (trimmedPromo == null)
            {
                var courierOnly = await _productRepo.PreviewCheckoutTotalsAsync(userId, guestId, null, "Courier");
                var pickupOnly = await _productRepo.PreviewCheckoutTotalsAsync(userId, guestId, null, "Pickup");
                return (courierOnly, pickupOnly, null);
            }

            var baseline = await _productRepo.PreviewCheckoutTotalsAsync(userId, guestId, null, "Courier");
            if (baseline == null)
                return (null, null, null);

            var promo = await _productRepo.GetPromoCodeByCodeAsync(trimmedPromo);
            var promoRejectReason = PromoCodeValidation.GetRejectReason(
                promo,
                trimmedPromo,
                baseline.GoodsTotalBeforePromo);

            if (promoRejectReason != null)
            {
                var pickupWithoutPromo = await _productRepo.PreviewCheckoutTotalsAsync(userId, guestId, null, "Pickup");
                return (baseline, pickupWithoutPromo, promoRejectReason);
            }

            var courier = await _productRepo.PreviewCheckoutTotalsAsync(userId, guestId, trimmedPromo, "Courier");
            var pickup = await _productRepo.PreviewCheckoutTotalsAsync(userId, guestId, trimmedPromo, "Pickup");
            return (courier, pickup, null);
        }

        private async Task FillPreviewAsync(CheckoutViewModel model, int? userId, string? guestId, string? promoCode)
        {
            var (courier, pickup, _) = await BuildCheckoutPreviewAsync(userId, guestId, promoCode);

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

            if (!string.IsNullOrWhiteSpace(model.PromoCode))
            {
                var baseline = await _productRepo.PreviewCheckoutTotalsAsync(
                    userId, guestId, null, model.DeliveryType ?? "Courier");
                var promo = await _productRepo.GetPromoCodeByCodeAsync(model.PromoCode.Trim());
                var promoRejectReason = PromoCodeValidation.GetRejectReason(
                    promo,
                    model.PromoCode.Trim(),
                    baseline?.GoodsTotalBeforePromo ?? 0m);

                if (promoRejectReason != null)
                {
                    ModelState.AddModelError(nameof(model.PromoCode), promoRejectReason);
                    model.CartItems = cartBeforePay;
                    await FillPreviewAsync(model, userId, guestId, null);
                    return View("Checkout", model);
                }
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
            catch (SqlException ex) when (ex.Number == 50001 || ex.Number == 50002 || ex.Number == 50003)
            {
                TempData["ToastMessage"] = ex.Number switch
                {
                    50002 => "Корзина пуста. Оформление отменено.",
                    50003 => "Промокод исчерпан.",
                    _ => "Нет товаров, доступных для заказа. Проверьте наличие."
                };
                TempData["ToastType"] = "error";
                return ex.Number == 50003
                    ? RedirectToAction("Checkout")
                    : RedirectToAction("Index", "Cart");
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
