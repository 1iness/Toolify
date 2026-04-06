using HouseholdStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HouseholdStore.Controllers
{
    [Authorize]
    public class FavouritesController : Controller
    {
        private readonly ProductApiService _productApi;

        public FavouritesController(ProductApiService productApi)
        {
            _productApi = productApi;
        }

        private int GetUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("id");
            int.TryParse(idStr, out int userId);
            return userId;
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId = GetUserId();
            var isFav = await _productApi.IsFavouriteAsync(userId, productId);

            if (isFav)
                await _productApi.RemoveFavouriteAsync(userId, productId);
            else
                await _productApi.AddFavouriteAsync(userId, productId);

            return Json(new { isFavourite = !isFav });
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var products = await _productApi.GetFavouritesAsync(userId);
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> GetJson()
        {
            var userId = GetUserId();
            var products = await _productApi.GetFavouritesAsync(userId);
            return Json(products);
        }
    }
}