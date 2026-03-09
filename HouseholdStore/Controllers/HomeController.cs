using HouseholdStore.Models;
using HouseholdStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using Toolify.ProductService.Models;

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
            var ratings = new Dictionary<int, double>();

            foreach (var product in products)
            {
                var reviews = await _productApi.GetReviewsAsync(product.Id);
                double avg = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
                ratings[product.Id] = avg;
            }

            ViewBag.ProductRatings = ratings;
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

            var reviews = await _productApi.GetReviewsAsync(id);

            ViewBag.Reviews = reviews; 
            ViewBag.ReviewsCount = reviews.Count;
            ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            var starCounts = new int[6]; 
            foreach (var r in reviews) starCounts[r.Rating]++;

            ViewBag.StarCounts = starCounts; 

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> LeaveReview(ReviewViewModel model)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int uid))
                {
                    model.UserId = uid;
                }
            }

            model.CreatedAt = DateTime.Now;

            try
            {
                await _productApi.AddReviewAsync(model);
                return RedirectToAction("Details", new { id = model.ProductId });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Details", new { id = model.ProductId });
            }
        }
        public async Task<IActionResult> AllReviews(int productId)
        {
            var product = await _productApi.GetByIdAsync(productId);
            if (product == null) return NotFound();

            var reviews = await _productApi.GetReviewsAsync(productId);

            ViewBag.Reviews = reviews;
            ViewBag.ReviewsCount = reviews.Count;

            ViewData["ParentPage"] = "Каталог";
            ViewData["ParentLink"] = "/Home/Index";
            ViewData["Title"] = $"Отзывы о {product.Name}";

            return View(product);
        }

        public async Task<IActionResult> Catalog(CatalogFilterViewModel filter)
        {
            var products = await _productApi.GetAllAsync();

            if (!string.IsNullOrEmpty(filter.SpecialCategory))
            {
                if (filter.SpecialCategory == "sale")
                    products = products.Where(p => p.Discount > 0).ToList();
                else if (filter.SpecialCategory == "hits")
                    products = products.OrderByDescending(p => p.StockQuantity).ToList();
            }

            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            {
                products = products.Where(p => p.CategoryId == filter.CategoryId.Value).ToList();

                ViewBag.CategoryFilters = await _productApi.GetCategoryFiltersAsync(filter.CategoryId.Value);
            }

            if (filter.SelectedFeatures != null && filter.SelectedFeatures.Any())
            {
                products = products.Where(p =>
                    filter.SelectedFeatures.All(selected =>
                        p.Configurations != null &&
                        p.Configurations.Any(c => c.FeatureId == selected.Key && selected.Value.Contains(c.FeatureValue))
                    )
                ).ToList();
            }

            if (filter.MinPrice.HasValue)
                products = products.Where(p => (p.Price * (100 - p.Discount) / 100) >= filter.MinPrice.Value).ToList();

            if (filter.MaxPrice.HasValue)
                products = products.Where(p => (p.Price * (100 - p.Discount) / 100) <= filter.MaxPrice.Value).ToList();

            products = filter.Sort switch
            {
                "price_asc" => products.OrderBy(p => p.Price * (100 - p.Discount) / 100).ToList(),
                "price_desc" => products.OrderByDescending(p => p.Price * (100 - p.Discount) / 100).ToList(),
                "name" => products.OrderBy(p => p.Name).ToList(),
                "rating" => products.OrderByDescending(p => p.StockQuantity).ToList(),
                _ => products
            };

            ViewBag.CategoryId = filter.CategoryId;
            ViewBag.SpecialCategory = filter.SpecialCategory;
            ViewBag.SelectedSort = filter.Sort;
            ViewBag.MinPrice = filter.MinPrice;
            ViewBag.MaxPrice = filter.MaxPrice;
            ViewBag.SelectedFeatures = filter.SelectedFeatures; 

            return View(products);
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
