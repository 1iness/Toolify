using HouseholdStore.Helpers;
using HouseholdStore.Models;
using HouseholdStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;
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
            var products = await _productApi.GetStoreCatalogAsync(GetCurrentUserId());
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

            var products = await _productApi.SearchProductsAsync(query, GetCurrentUserId());
            return Json(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _productApi.GetStoreProductByIdAsync(id, GetCurrentUserId());
            if (product == null)
            {
                return NotFound();
            }

            var reviews = await _productApi.GetReviewsAsync(id);

            ViewBag.Reviews = reviews; 
            ViewBag.ReviewsCount = reviews.Count;
            ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            var starCounts = new int[6];
            foreach (var r in reviews)
            {
                var b = (int)Math.Round(r.Rating, MidpointRounding.AwayFromZero);
                b = Math.Clamp(b, 1, 5);
                starCounts[b]++;
            }

            ViewBag.StarCounts = starCounts;


            bool isFavourite = false;
            if (User.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("id");
                if (int.TryParse(idStr, out int uid))
                    isFavourite = await _productApi.IsFavouriteAsync(uid, id);
            }
            ViewBag.IsFavourite = isFavourite;
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> LeaveReview(ReviewViewModel model)
        {
            var ratingRaw = Request.Form["Rating"].ToString();
            if (!string.IsNullOrEmpty(ratingRaw) &&
                double.TryParse(ratingRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var ratingParsed))
            {
                model.Rating = ratingParsed;
            }

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
            var product = await _productApi.GetStoreProductByIdAsync(productId, GetCurrentUserId());
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
            var products = await _productApi.GetStoreCatalogAsync(GetCurrentUserId());

            if (!string.IsNullOrEmpty(filter.SpecialCategory))
            {
                if (filter.SpecialCategory == "sale")
                    products = products.Where(p => CatalogProductPricing.ShowDiscountStyle(p)).ToList();
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
                products = products.Where(p => CatalogProductPricing.FinalUnitPrice(p) >= filter.MinPrice.Value).ToList();

            if (filter.MaxPrice.HasValue)
                products = products.Where(p => CatalogProductPricing.FinalUnitPrice(p) <= filter.MaxPrice.Value).ToList();

            products = filter.Sort switch
            {
                "price_asc" => products.OrderBy(p => CatalogProductPricing.FinalUnitPrice(p)).ToList(),
                "price_desc" => products.OrderByDescending(p => CatalogProductPricing.FinalUnitPrice(p)).ToList(),
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

        [HttpGet]
        public async Task<IActionResult> SmartSelection(int? categoryId)
        {
            ViewBag.Categories = await _productApi.GetCategoriesAsync();
            if (categoryId.HasValue)
            {
                ViewBag.SelectedCategoryId = categoryId.Value;
                ViewBag.CategoryFilters = await _productApi.GetCategoryFiltersAsync(categoryId.Value);
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SmartSelectionResults(CatalogFilterViewModel filter)
        {
            var products = await _productApi.GetStoreCatalogAsync(GetCurrentUserId());

            if (filter.CategoryId.HasValue)
                products = products.Where(p => p.CategoryId == filter.CategoryId.Value).ToList();

            if (filter.MinPrice.HasValue)
                products = products.Where(p => CatalogProductPricing.FinalUnitPrice(p) >= filter.MinPrice.Value).ToList();

            if (filter.MaxPrice.HasValue)
                products = products.Where(p => CatalogProductPricing.FinalUnitPrice(p) <= filter.MaxPrice.Value).ToList();

            if (filter.SelectedFeatures != null && filter.SelectedFeatures.Any())
            {
                products = products.Where(p =>
                    filter.SelectedFeatures.All(selected =>
                        p.Configurations != null &&
                        p.Configurations.Any(c => c.FeatureId == selected.Key && selected.Value.Contains(c.FeatureValue))
                    )
                ).ToList();
            }
            var topProducts = products.OrderByDescending(p => p.StockQuantity).Take(4).ToList();

            var uniqueFeatures = topProducts
                .SelectMany(p => p.Configurations ?? new List<ProductConfiguration>())
                .Select(c => c.FeatureName)
                .Distinct()
                .ToList();

            ViewBag.UniqueFeatures = uniqueFeatures;

            return View("Compare", topProducts);
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
        private int? GetCurrentUserId()
        {
            if (User.Identity?.IsAuthenticated != true) return null;
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("id");
            return int.TryParse(idStr, out var uid) ? uid : null;
        }
    }
}
