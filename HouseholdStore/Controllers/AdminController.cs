using HouseholdStore.Models;
using HouseholdStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Toolify.AuthService.Services;
using System.Text;
using Toolify.ProductService.Data;
using Toolify.ProductService.Models;
using static System.Net.Mime.MediaTypeNames;

namespace HouseholdStore.Controllers
{

    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ProductApiService _api;
        private readonly AuthApiService _authApi;
        private readonly ProductRepository _repo;
        private readonly EmailService _email;

        public AdminController(ProductApiService api, AuthApiService authApi, ProductRepository repo, EmailService email)
        {
            _api = api;
            _authApi = authApi;
            _repo = repo;
            _email = email;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _api.GetAllAsync();
            return View(products);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> List()
        {
            var products = await _api.GetAllAsync();
            return View(products);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _api.GetByIdAsync(id);
            if (product == null) return NotFound();

            var categories = await _api.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product product, IFormFile? image)
        {
            if (product.Configurations != null && product.Configurations.Any())
            {
                foreach (var config in product.Configurations)
                {
                    if (config.FeatureId == 0 && !string.IsNullOrWhiteSpace(config.FeatureName))
                    {
                        var createdFeature = await _api.AddFeatureToCategoryAsync(product.CategoryId, config.FeatureName);
                        if (createdFeature != null)
                        {
                            config.FeatureId = createdFeature.Id;
                        }
                    }
                }
            }

            ModelState.Remove("image");
            ModelState.Remove("ArticleNumber");

            if (ModelState.IsValid)
            {
                await _api.UpdateAsync(product);

                if (product.Configurations != null)
                {
                    await _api.UpdateConfigurationsAsync(product.Id, product.Configurations);
                }

                if (image != null)
                {
                    await _api.UploadImageAsync(product.Id, image);
                }

                return RedirectToAction("Index");
            }

            var categories = await _api.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }


        public async Task<IActionResult> Create()
        {
            var categories = await _api.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(new Product());

        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile? image, string? NewCategoryName)
        {
            var rnd = new Random();
            product.ArticleNumber = rnd.Next(10000, 99999).ToString();

            if (!string.IsNullOrWhiteSpace(NewCategoryName))
            {
                var newCat = new Category { Name = NewCategoryName };
                var createdCat = await _api.CreateCategoryAsync(newCat);
                product.CategoryId = createdCat.Id;
                ModelState.Remove("CategoryId");
            }
            else if (product.CategoryId <= 0)
            {
                ModelState.AddModelError("CategoryId", "Выберите категорию или создайте новую!");
            }

            if (product.CategoryId > 0 && product.Configurations != null && product.Configurations.Any())
            {
                foreach (var config in product.Configurations)
                {
                    if (config.FeatureId == 0 && !string.IsNullOrWhiteSpace(config.FeatureName))
                    {
                        var createdFeature = await _api.AddFeatureToCategoryAsync(product.CategoryId, config.FeatureName);
                        if (createdFeature != null)
                        {
                            config.FeatureId = createdFeature.Id;
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                var newProductId = await _api.CreateAsync(product);
                if (image != null && newProductId.HasValue)
                {
                    await _api.UploadImageAsync(newProductId.Value, image);
                }
                return RedirectToAction("Index");
            }

            var categories = await _api.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _api.DeleteAsync(id);
            return RedirectToAction("Index");
        }

        [HttpGet("Admin/GetFeatures")]
        public async Task<IActionResult> GetFeatures(int categoryId)
        {
            var features = await _api.GetFeaturesByCategoryAsync(categoryId);
            return Json(features);
        }

        [HttpGet]
        public async Task<IActionResult> PromoCodes()
        {
            var promos = await _api.GetAllPromoCodesAsync();
            return View(promos);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePromoCode(string code, int discountPercent, DateTime startDate, DateTime endDate, int? maxUses = null)
        {
            if (!string.IsNullOrEmpty(code) && discountPercent > 0)
            {
                if (maxUses.HasValue && maxUses.Value < 1)
                {
                    TempData["Success"] = null;
                    TempData["Error"] = "Лимит использований должен быть не меньше 1 или оставьте поле пустым.";
                    return RedirectToAction("PromoCodes");
                }

                await _api.CreatePromoCodeAsync(code, discountPercent, startDate, endDate, maxUses);
                TempData["Success"] = "Промокод успешно добавлен";
            }
            return RedirectToAction("PromoCodes");
        }
        [HttpGet]
        public async Task<IActionResult> Discounts()
        {
            var campaigns = await _api.GetDiscountCampaignsAsync();
            var rules = await _api.GetDiscountRulesAsync();
            var categories = await _api.GetCategoriesAsync();
            var products = await _api.GetAllAsync();
            List<Toolify.AuthService.Models.User> users;
            try
            {
                users = await _authApi.GetAllUsersAsync();
            }
            catch (Exception ex)
            {
                users = new List<Toolify.AuthService.Models.User>();
                TempData["Error"] = ex.Message;
            }

            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.Users = new SelectList(users, "Id", "Email");
            ViewBag.Products = new SelectList(products, "Id", "Name");
            ViewBag.CategoriesList = categories;
            ViewBag.UsersList = users;
            ViewBag.ProductsList = products;

            return View(new AdminDiscountsViewModel { Campaigns = campaigns, Rules = rules });
        }

        [HttpPost]
        public async Task<IActionResult> AddDiscountCampaign(string name, string? description, DateTime startDate, DateTime endDate, bool isActive = true, int priority = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Укажите название акции";
                return RedirectToAction("Discounts");
            }

            var campaign = new DiscountCampaign
            {
                Name = name.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                StartDate = startDate,
                EndDate = endDate,
                IsActive = isActive,
                Priority = priority
            };
            var (ok, err) = await _api.UpsertDiscountCampaignAsync(false, campaign);
            TempData[ok ? "Success" : "Error"] = ok ? "Акция добавлена" : (err ?? "Ошибка API");
            return RedirectToAction("Discounts");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDiscountCampaign(int id, string name, string? description, DateTime startDate, DateTime endDate, bool isActive, int priority)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Укажите название акции";
                return RedirectToAction("Discounts");
            }

            var campaign = new DiscountCampaign
            {
                Id = id,
                Name = name.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                StartDate = startDate,
                EndDate = endDate,
                IsActive = isActive,
                Priority = priority
            };
            var (ok, err) = await _api.UpsertDiscountCampaignAsync(true, campaign);
            TempData[ok ? "Success" : "Error"] = ok ? "Акция обновлена" : (err ?? "Ошибка API");
            return RedirectToAction("Discounts");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDiscountCampaign(int id)
        {
            var (ok, err) = await _api.DeleteDiscountCampaignAsync(id);
            TempData[ok ? "Success" : "Error"] = ok ? "Акция удалена" : (err ?? "Ошибка API");
            return RedirectToAction("Discounts");
        }

        [HttpPost]
        public async Task<IActionResult> AddDiscountRule(string scopeType, string discountMode, decimal discountValue, int? campaignId, int? productId, int? categoryId, int? userId, decimal? minGoodsAmount, int? bundleBuyQty, int? bundlePayQty, bool isActive = true)
        {
            var rule = new DiscountRule
            {
                ScopeType = scopeType,
                DiscountMode = discountMode,
                DiscountValue = discountMode == "Bundle" ? 0 : discountValue,
                CampaignId = campaignId,
                ProductId = scopeType == "Product" ? productId : null,
                CategoryId = scopeType == "Category" ? categoryId : null,
                UserId = scopeType == "Client" ? userId : null,
                MinGoodsAmount = minGoodsAmount,
                BundleBuyQty = discountMode == "Bundle" ? bundleBuyQty : null,
                BundlePayQty = discountMode == "Bundle" ? bundlePayQty : null,
                IsActive = isActive
            };
            var (ok, err) = await _api.UpsertDiscountRuleAsync(false, rule);
            TempData[ok ? "Success" : "Error"] = ok ? "Правило добавлено" : (err ?? "Ошибка API");
            return RedirectToAction("Discounts");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDiscountRule(int id, string scopeType, string discountMode, decimal discountValue, int? campaignId, int? productId, int? categoryId, int? userId, decimal? minGoodsAmount, int? bundleBuyQty, int? bundlePayQty, bool isActive)
        {
            var rule = new DiscountRule
            {
                Id = id,
                ScopeType = scopeType,
                DiscountMode = discountMode,
                DiscountValue = discountMode == "Bundle" ? 0 : discountValue,
                CampaignId = campaignId,
                ProductId = scopeType == "Product" ? productId : null,
                CategoryId = scopeType == "Category" ? categoryId : null,
                UserId = scopeType == "Client" ? userId : null,
                MinGoodsAmount = minGoodsAmount,
                BundleBuyQty = discountMode == "Bundle" ? bundleBuyQty : null,
                BundlePayQty = discountMode == "Bundle" ? bundlePayQty : null,
                IsActive = isActive
            };
            var (ok, err) = await _api.UpsertDiscountRuleAsync(true, rule);
            TempData[ok ? "Success" : "Error"] = ok ? "Правило обновлено" : (err ?? "Ошибка API");
            return RedirectToAction("Discounts");
        }

        [HttpGet]
        public async Task<IActionResult> GetDiscountProductStatus(int productId)
        {
            try
            {
                var status = await _api.GetDiscountProductStatusAsync(productId);
                return Json(status);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDiscountRule(int id)
        {
            var (ok, err) = await _api.DeleteDiscountRuleAsync(id);
            TempData[ok ? "Success" : "Error"] = ok ? "Правило удалено" : (err ?? "Ошибка API");
            return RedirectToAction("Discounts");
        }


        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            var users = await _authApi.GetAllUsersAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Orders(DateTime? startDate, DateTime? endDate)
        {
            var orders = await _api.GetAllOrdersAsync();

            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Начальная дата не может быть больше конечной.";

                startDate = null;
                endDate = null;
            }

            if (startDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate.Date >= startDate.Value.Date).ToList();
            }

            if (endDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate.Date <= endDate.Value.Date).ToList();
            }

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var allOrders = await _api.GetAllOrdersAsync();
            var order = allOrders.FirstOrDefault(o => o.Id == orderId);
            var previousStatus = order?.Status;

            if (order != null && string.Equals(previousStatus, status, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ToastType"] = "info";
                TempData["ToastMessage"] = $"Статус заказа #{orderId} уже установлен: '{status}'";
                return RedirectToAction("Orders");
            }

            await _api.UpdateOrderStatusAsync(orderId, status);

            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = $"Статус заказа #{orderId} изменен на '{status}'";

            try
            {
                string? toEmail = order?.GuestEmail;

                if (string.IsNullOrWhiteSpace(toEmail) && order?.UserId != null)
                {
                    var users = await _authApi.GetAllUsersAsync();
                    toEmail = users.FirstOrDefault(u => u.Id == order.UserId.Value)?.Email;
                }

                if (!string.IsNullOrWhiteSpace(toEmail))
                {
                    var lines = (order?.Items ?? new List<OrderItem>())
                        .Select(i => new OrderLine
                        {
                            Name = i.ProductName,
                            Quantity = i.Quantity,
                            Price = i.Price
                        })
                        .ToList();

                    await _email.SendOrderStatusChangedAsync(
                        toEmail,
                        orderId,
                        previousStatus,
                        status,
                        order?.Address,
                        order?.TotalAmount ?? 0m,
                        lines);
                }
            }
            catch
            {
            }

            return RedirectToAction("Orders");
        }


        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var model = new DashboardViewModel
            {
                OrdersByStatus = await _repo.GetOrdersByStatusReportAsync(),
                ProductsByCategory = await _repo.GetProductsByCategoryReportAsync(),
                ClientHistory = await _repo.GetClientHistoryReportAsync()
            };
            return View(model);
        }

        private FileResult GenerateCsv(string csvContent, string fileName)
        {
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var bytes = Encoding.UTF8.GetBytes(csvContent);
            var finalBytes = bom.Concat(bytes).ToArray();
            return File(finalBytes, "text/csv", fileName);
        }

        public async Task<IActionResult> ExportOrdersByStatus()
        {
            var data = await _repo.GetOrdersByStatusReportAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Статус,Количество заказов");
            foreach (var item in data) sb.AppendLine($"\"{item.Status}\",{item.OrderCount}");
            return GenerateCsv(sb.ToString(), "OrdersByStatus.csv");
        }

        public async Task<IActionResult> ExportProductsByCategory()
        {
            var data = await _repo.GetProductsByCategoryReportAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Категория,Количество товаров");
            foreach (var item in data) sb.AppendLine($"\"{item.CategoryName}\",{item.ProductCount}");
            return GenerateCsv(sb.ToString(), "ProductsByCategory.csv");
        }

        public async Task<IActionResult> ExportClientHistory()
        {
            var data = await _repo.GetClientHistoryReportAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Клиент,Email,Всего заказов,Общая сумма (BYN),Дата последнего заказа");
            foreach (var item in data) sb.AppendLine($"\"{item.ClientName}\",\"{item.ClientEmail}\",{item.TotalOrders},{item.TotalSpent.ToString("F2")},{item.LastOrderDate.ToShortDateString()}");
            return GenerateCsv(sb.ToString(), "ClientHistory.csv");
        }
    }
}

