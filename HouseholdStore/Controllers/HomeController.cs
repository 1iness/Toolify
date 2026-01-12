using HouseholdStore.Models;
using HouseholdStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HouseholdStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ProductApiService _productApi;

        public HomeController(ILogger<HomeController> logger, ProductApiService productApi)
        {
            _logger = logger;
            _productApi = productApi;
        }
        public async Task<IActionResult> Index()
        {
            var products = await _productApi.GetAllAsync();

            return View(products);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [Authorize]
        public IActionResult Secret()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchJson(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Json(new List<object>()); 

            var products = await _productApi.SearchProductsAsync(query);
            return Json(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _productApi.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminOnly()
        {
            return Content("админ ура ура админ!");
        }

        public IActionResult About()
        {
            return View();
        }
    }
}
