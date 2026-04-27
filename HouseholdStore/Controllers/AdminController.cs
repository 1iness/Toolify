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
        private readonly AdminReportBuilder _reportBuilder;
        private readonly AdminReportExportService _reportExporter;

        public AdminController(
            ProductApiService api,
            AuthApiService authApi,
            ProductRepository repo,
            EmailService email,
            AdminReportBuilder reportBuilder,
            AdminReportExportService reportExporter)
        {
            _api = api;
            _authApi = authApi;
            _repo = repo;
            _email = email;
            _reportBuilder = reportBuilder;
            _reportExporter = reportExporter;
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
        public async Task<IActionResult> CreatePromoCode(string code, int discountPercent, DateTime startDate, DateTime endDate, int? maxUses = null, decimal? minGoodsAmount = null)
        {
            if (!string.IsNullOrEmpty(code) && discountPercent > 0)
            {
                if (maxUses.HasValue && maxUses.Value < 1)
                {
                    TempData["Success"] = null;
                    TempData["Error"] = "Лимит использований должен быть не меньше 1 или оставьте поле пустым.";
                    return RedirectToAction("PromoCodes");
                }

                if (minGoodsAmount.HasValue && minGoodsAmount.Value < 0)
                {
                    TempData["Success"] = null;
                    TempData["Error"] = "Минимальная сумма не может быть отрицательной.";
                    return RedirectToAction("PromoCodes");
                }

                await _api.CreatePromoCodeAsync(code, discountPercent, startDate, endDate, maxUses, minGoodsAmount);
                TempData["Success"] = "Промокод успешно добавлен";
            }
            return RedirectToAction("PromoCodes");
        }

        [HttpGet]
        public async Task<IActionResult> Promotions()
        {
            var promos = await _api.GetPromotionsAsync();
            var categories = await _api.GetCategoriesAsync();
            var products = await _api.GetAllAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.Products = new SelectList(products, "Id", "Name");
            ViewBag.CategoriesList = categories;
            ViewBag.ProductsList = products;

            return View(new AdminPromotionsViewModel { Promotions = promos });
        }

        [HttpPost]
        public async Task<IActionResult> AddPromotion(Promotion model)
        {
            var (ok, err) = await _api.UpsertPromotionAsync(false, Sanitize(model));
            TempData[ok ? "Success" : "Error"] = ok ? "Акция добавлена" : (err ?? "Ошибка API");
            return RedirectToAction("Promotions");
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePromotion(Promotion model)
        {
            var (ok, err) = await _api.UpsertPromotionAsync(true, Sanitize(model));
            TempData[ok ? "Success" : "Error"] = ok ? "Акция обновлена" : (err ?? "Ошибка API");
            return RedirectToAction("Promotions");
        }

        [HttpPost]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            var (ok, err) = await _api.DeletePromotionAsync(id);
            TempData[ok ? "Success" : "Error"] = ok ? "Акция удалена" : (err ?? "Ошибка API");
            return RedirectToAction("Promotions");
        }

        [HttpGet]
        public async Task<IActionResult> GetPromotionProductStatus(int productId)
        {
            try
            {
                var status = await _api.GetPromotionProductStatusAsync(productId);
                return Json(status);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { error = ex.Message });
            }
        }

        private static Promotion Sanitize(Promotion p)
        {
            p.Name = (p.Name ?? string.Empty).Trim();
            p.Description = string.IsNullOrWhiteSpace(p.Description) ? null : p.Description!.Trim();
            p.GiftDescription = string.IsNullOrWhiteSpace(p.GiftDescription) ? null : p.GiftDescription!.Trim();

            if (p.ScopeType != PromotionScopes.Category) p.CategoryId = null;
            if (p.ScopeType != PromotionScopes.Product) p.ProductId = null;

            if (p.PromotionType != PromotionTypes.BuyGetY) { p.BuyQty = null; p.PayQty = null; }
            if (p.PromotionType != PromotionTypes.OrderPercent) p.PercentOff = null;
            if (p.PromotionType != PromotionTypes.Gift) p.GiftDescription = null;
            if (p.PromotionType == PromotionTypes.BuyGetY || p.PromotionType == PromotionTypes.Gift)
                p.MinOrderAmount = null;
            return p;
        }


        [HttpGet]
        public async Task<IActionResult> Discounts()
        {
            var discounts = await _api.GetDiscountsAsync();
            var categories = await _api.GetCategoriesAsync();
            var products = await _api.GetAllAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.Products = new SelectList(products, "Id", "Name");
            ViewBag.CategoriesList = categories;
            ViewBag.ProductsList = products;

            return View(new AdminDiscountsViewModel { Discounts = discounts });
        }

        [HttpPost]
        public async Task<IActionResult> AddDiscount(Discount model)
        {
            var (ok, err) = await _api.UpsertDiscountAsync(false, SanitizeDiscount(model));
            TempData[ok ? "Success" : "Error"] = ok ? "Скидка добавлена" : (err ?? "Ошибка API");
            return RedirectToAction("Discounts");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDiscount(Discount model)
        {
            var (ok, err) = await _api.UpsertDiscountAsync(true, SanitizeDiscount(model));
            TempData[ok ? "Success" : "Error"] = ok ? "Скидка обновлена" : (err ?? "Ошибка API");
            return RedirectToAction("Discounts");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDiscount(int id)
        {
            var (ok, err) = await _api.DeleteDiscountAsync(id);
            TempData[ok ? "Success" : "Error"] = ok ? "Скидка удалена" : (err ?? "Ошибка API");
            return RedirectToAction("Discounts");
        }

        private static Discount SanitizeDiscount(Discount d)
        {
            d.Name = (d.Name ?? string.Empty).Trim();

            if (d.DiscountType == DiscountTypes.Product)
            {
                d.CategoryId = null;
                d.MinQuantity = null;
            }
            else if (d.DiscountType == DiscountTypes.Category)
            {
                d.ProductId = null;
                d.MinQuantity = null;
            }

            return d;
        }


        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            try
            {
                var users = await _authApi.GetAllUsersAsync();
                return View(users);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(new List<Toolify.AuthService.Models.User>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeUserRole(int userId, string role)
        {
            try
            {
                await _authApi.ChangeUserRoleAsync(userId, role);
                TempData["Success"] = "Роль пользователя обновлена";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Clients");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserBlocked(int userId, bool isBlocked)
        {
            try
            {
                await _authApi.SetUserBlockedAsync(userId, isBlocked);
                TempData["Success"] = isBlocked ? "Пользователь заблокирован" : "Пользователь разблокирован";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Clients");
        }

        [HttpPost]
        public async Task<IActionResult> SendPasswordReset(string email)
        {
            try
            {
                await _authApi.SendPasswordResetAsync(email);
                TempData["Success"] = $"Письмо для сброса пароля отправлено на {email}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Clients");
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

        [HttpGet]
        public async Task<IActionResult> Reports(AdminReportsFilter filter)
        {
            if (AdminReportBuilder.HasInvalidDateRanges(filter, out var message))
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = message;
                filter = new AdminReportsFilter();
            }

            var model = await _reportBuilder.BuildAsync(filter);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportReports(AdminReportsFilter filter, string format, string reportType = "all")
        {
            if (AdminReportBuilder.HasInvalidDateRanges(filter, out var message))
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = message;
                return RedirectToAction("Reports");
            }

            var model = await _reportBuilder.BuildAsync(filter);
            var tables = _reportBuilder.BuildTables(model, reportType);
            var fileStamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            var reportName = (reportType ?? "all").ToLowerInvariant() switch
            {
                "sales" => "Sales",
                "average" => "AverageCheck",
                "popularity" => "ProductPopularity",
                "customers" => "CustomerHistory",
                _ => "Reports"
            };

            return format?.ToLowerInvariant() switch
            {
                "pdf" => File(_reportExporter.ExportPdf(tables), "application/pdf", $"Toolify_{reportName}_{fileStamp}.pdf"),
                "word" or "docx" => File(
                    _reportExporter.ExportWord(tables),
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    $"Toolify_{reportName}_{fileStamp}.docx"),
                "excel" or "xlsx" => File(
                    _reportExporter.ExportExcel(tables),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Toolify_{reportName}_{fileStamp}.xlsx"),
                _ => BadRequest("Неизвестный формат экспорта.")
            };
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

