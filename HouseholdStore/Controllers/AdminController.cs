using HouseholdStore.Helpers;
using HouseholdStore.Models;
using HouseholdStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Toolify.AuthService.Services;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Toolify.ProductService;
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
        private readonly IWebHostEnvironment _env;

        public AdminController(
            ProductApiService api,
            AuthApiService authApi,
            ProductRepository repo,
            EmailService email,
            AdminReportBuilder reportBuilder,
            AdminReportExportService reportExporter,
            IWebHostEnvironment env)
        {
            _api = api;
            _authApi = authApi;
            _repo = repo;
            _email = email;
            _reportBuilder = reportBuilder;
            _reportExporter = reportExporter;
            _env = env;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Админ-панель";
            ViewBag.AdminSectionTitle = "Панель управления";
            ViewBag.AdminPanelKey = "home";
            ViewBag.AdminSearchTarget = "none";
            if (AdminPartialHelper.IsPartial(Request))
            {
                Response.Headers["X-Admin-Panel"] = "home";
                Response.Headers["X-Admin-Search"] = "none";
                Response.Headers["X-Admin-Page-Title"] = AdminPageTitleHelper.EncodeForHeader("Панель управления");
                return PartialView("_AdminHomeInner");
            }

            return View();
        }

        public async Task<IActionResult> List()
        {
            var products = await _api.GetAllAsync();
            return AdminShellView(products, "products-list", "products-list");
        }

        public async Task<IActionResult> Categories()
        {
            var list = (await _api.GetCategoriesForAdminAsync()).OrderByDescending(c => c.Id).ToList();
            var featuresByCategory = new Dictionary<int, List<ProductFeature>>();
            foreach (var category in list)
            {
                featuresByCategory[category.Id] = await _api.GetFeaturesByCategoryAsync(category.Id);
            }

            ViewBag.CategoryFeatures = featuresByCategory;
            return AdminShellView(list, "products-categories", "products-categories");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string name, List<string>? featureNames)
        {
            name = (name ?? string.Empty).Trim();
            var normalizedFeatures = (featureNames ?? new List<string>())
                .Select(x => (x ?? string.Empty).Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["CategoryError"] = "Укажите название категории.";
                return RedirectToAction(nameof(Categories));
            }

            var createResult = await _api.CreateCategoryAsync(new Category { Name = name });
            if (!createResult.IsSuccess || createResult.Category == null)
            {
                TempData["CategoryError"] = createResult.ErrorMessage
                    ?? "Не удалось создать категорию.";
                return RedirectToAction(nameof(Categories));
            }

            foreach (var feature in normalizedFeatures)
            {
                await _api.AddFeatureToCategoryAsync(createResult.Category.Id, feature);
            }

            TempData["CategoryMessage"] = normalizedFeatures.Count > 0
                ? $"Категория «{createResult.Category.Name}» создана. Характеристик: {normalizedFeatures.Count}."
                : $"Категория «{createResult.Category.Name}» создана.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var (ok, error) = await _api.DeleteCategoryAsync(id);
            if (ok)
            {
                var iconsDir = Path.Combine(_env.WebRootPath, "image", "category-icons");
                if (Directory.Exists(iconsDir))
                {
                    foreach (var f in Directory.GetFiles(iconsDir, $"{id}.*"))
                    {
                        try { System.IO.File.Delete(f); } catch { }
                    }
                }
                TempData["CategoryMessage"] = "Категория удалена.";
            }
            else TempData["CategoryError"] = error ?? "Не удалось удалить категорию";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(2_000_000)]
        public async Task<IActionResult> UploadCategoryIcon(int id, IFormFile? iconFile)
        {
            if (iconFile == null || iconFile.Length == 0)
            {
                TempData["CategoryError"] = "Выберите файл изображения.";
                return RedirectToAction(nameof(Categories));
            }
            var ext = Path.GetExtension(iconFile.FileName).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".webp" && ext != ".gif")
            {
                TempData["CategoryError"] = "Допустимы форматы: png, jpg, jpeg, webp, gif.";
                return RedirectToAction(nameof(Categories));
            }
            var fileName = $"{id}{ext}";
            var dir = Path.Combine(_env.WebRootPath, "image", "category-icons");
            Directory.CreateDirectory(dir);
            var fullPath = Path.Combine(dir, fileName);
            foreach (var old in Directory.GetFiles(dir, $"{id}.*"))
            {
                if (!string.Equals(old, fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    try { System.IO.File.Delete(old); } catch {  }
                }
            }
            await using (var stream = new FileStream(fullPath, FileMode.Create))
                await iconFile.CopyToAsync(stream);

            if (!await _api.SetCategoryIconFileNameAsync(id, fileName))
            {
                TempData["CategoryError"] = "Файл сохранён, но в БД не записано имя.";
                return RedirectToAction(nameof(Categories));
            }
            TempData["CategoryMessage"] = "Иконка категории обновлена.";
            return RedirectToAction(nameof(Categories));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _api.GetByIdAsync(id);
            if (product == null) return NotFound();

            var categories = await _api.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);

            return AdminShellView(product, "products-edit", "none");
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product product, IFormFile? image)
        {
            ApplyStockQuantityFromForm(product);
            RemoveProductBindingNoise();
            ValidateProductForAdmin(product);

            if (product.Configurations != null && product.Configurations.Any())
            {
                foreach (var config in product.Configurations)
                {
                    if (config.FeatureId == 0 && !string.IsNullOrWhiteSpace(config.FeatureName))
                    {
                        var createdFeature = await _api.AddFeatureToCategoryAsync(product.CategoryId, config.FeatureName, isTemplate: false);
                        if (createdFeature != null)
                        {
                            config.FeatureId = createdFeature.Id;
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var updated = await _api.UpdateAsync(product);
                    if (!updated)
                    {
                        ModelState.AddModelError(string.Empty, "Не удалось сохранить товар. Проверьте данные и попробуйте ещё раз.");
                    }
                    else
                    {
                        if (image != null)
                        {
                            await _api.UploadImageAsync(product.Id, image);
                        }

                        return RedirectToAction("Index");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ToAdminProductError(ex.Message));
                }
            }

            var categories = await _api.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return AdminShellView(product, "products-edit", "none");
        }


        public async Task<IActionResult> Create()
        {
            var categories = await _api.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return AdminShellView(new Product(), "products-create", "none");

        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile? image, string? NewCategoryName)
        {
            ApplyStockQuantityFromForm(product);
            RemoveProductBindingNoise();
            ValidateProductImageForCreate(image);

            var rnd = new Random();
            product.ArticleNumber = rnd.Next(10000, 99999).ToString();

            if (product.CategoryId <= 0)
            {
                ModelState.AddModelError("CategoryId", "Выберите категорию. Если её нет, сначала создайте её в разделе «Категории».");
            }

            ValidateProductForAdmin(product);

            if (product.CategoryId > 0 && product.Configurations != null && product.Configurations.Any())
            {
                foreach (var config in product.Configurations)
                {
                    if (config.FeatureId == 0 && !string.IsNullOrWhiteSpace(config.FeatureName))
                    {
                        var createdFeature = await _api.AddFeatureToCategoryAsync(product.CategoryId, config.FeatureName, isTemplate: false);
                        if (createdFeature != null)
                        {
                            config.FeatureId = createdFeature.Id;
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var newProductId = await _api.CreateAsync(product);
                    if (image != null && newProductId.HasValue)
                    {
                        await _api.UploadImageAsync(newProductId.Value, image);
                    }
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ToAdminProductError(ex.Message));
                }
            }

            var categories = await _api.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return AdminShellView(product, "products-create", "none");
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _api.DeleteAsync(id);
                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = "Товар удалён.";
            }
            catch (Exception ex)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetProductCatalogVisibility(int id, bool isHiddenFromCatalog)
        {
            try
            {
                await _api.SetCatalogVisibilityAsync(id, isHiddenFromCatalog);
                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = isHiddenFromCatalog
                    ? "Товар скрыт из каталога."
                    : "Товар снова показывается в каталоге.";
            }
            catch (Exception ex)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(List));
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
            return AdminShellView(promos, "promocodes", "none");
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

                var (ok, error) = await _api.CreatePromoCodeAsync(code, discountPercent, startDate, endDate, maxUses, minGoodsAmount);
                if (!ok)
                {
                    TempData["Success"] = null;
                    TempData["Error"] = error ?? "Ошибка API";
                    return RedirectToAction("PromoCodes");
                }

                TempData["Success"] = "Промокод успешно добавлен"
                    + await TryNotifyUsersAboutCreatedOfferAsync("промокод", code);
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

            return AdminShellView(new AdminPromotionsViewModel { Promotions = promos }, "promotions", "none");
        }

        [HttpPost]
        public async Task<IActionResult> AddPromotion(Promotion model)
        {
            var promotion = Sanitize(model);
            var (ok, err) = await _api.UpsertPromotionAsync(false, promotion);
            TempData[ok ? "Success" : "Error"] = ok
                ? "Акция добавлена" + await TryNotifyUsersAboutCreatedOfferAsync("акция", promotion.Name)
                : (err ?? "Ошибка API");
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

            return AdminShellView(new AdminDiscountsViewModel { Discounts = discounts }, "discounts", "none");
        }

        [HttpPost]
        public async Task<IActionResult> AddDiscount(Discount model)
        {
            var discount = SanitizeDiscount(model);
            var (ok, err) = await _api.UpsertDiscountAsync(false, discount);
            TempData[ok ? "Success" : "Error"] = ok
                ? "Скидка добавлена" + await TryNotifyUsersAboutCreatedOfferAsync("скидка", discount.Name)
                : (err ?? "Ошибка API");
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

        private async Task<string> TryNotifyUsersAboutCreatedOfferAsync(string offerType, string? offerName)
        {
            try
            {
                var recipients = (await _authApi.GetAllUsersAsync())
                    .Where(u =>
                        !string.IsNullOrWhiteSpace(u.Email) &&
                        string.Equals(u.Role, "User", StringComparison.OrdinalIgnoreCase))
                    .Select(u => u.Email.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (recipients.Count == 0)
                    return ". Пользователей для рассылки не найдено.";

                var sent = 0;
                var failed = 0;
                foreach (var email in recipients)
                {
                    try
                    {
                        await _email.SendMarketingItemCreatedAsync(email, offerType, offerName ?? string.Empty);
                        sent++;
                    }
                    catch
                    {
                        failed++;
                    }
                }

                return failed == 0
                    ? $". Рассылка отправлена: {sent}."
                    : $". Рассылка отправлена: {sent}, ошибок: {failed}.";
            }
            catch
            {
                return ". Не удалось отправить рассылку пользователям.";
            }
        }


        [HttpGet]
        public async Task<IActionResult> Clients()
        {
            try
            {
                var users = await _authApi.GetAllUsersAsync();
                return AdminShellView(users, "clients", "clients");
            }
            catch (UnauthorizedAccessException ex)
            {
                Response.Cookies.Delete("jwt");
                TempData["Error"] = ex.Message;
                return AdminShellView(new List<Toolify.AuthService.Models.User>(), "clients", "clients");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return AdminShellView(new List<Toolify.AuthService.Models.User>(), "clients", "clients");
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
            catch (UnauthorizedAccessException ex)
            {
                Response.Cookies.Delete("jwt");
                TempData["Error"] = ex.Message;
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
            catch (UnauthorizedAccessException ex)
            {
                Response.Cookies.Delete("jwt");
                TempData["Error"] = ex.Message;
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

            return AdminShellView(orders, "orders", "orders");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var allOrders = await _api.GetAllOrdersAsync();
            var order = allOrders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = $"Заказ №{orderId} не найден.";
                return RedirectToAction(nameof(Orders));
            }

            if (!OrderStatusWorkflow.TryNormalize(order.Status, out var prevCanon))
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] =
                    $"Текущий статус заказа №{orderId} («{order.Status}») не распознан системой.";
                return RedirectToAction(nameof(Orders));
            }

            if (!OrderStatusWorkflow.TryNormalize(status, out var nextCanon))
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Недопустимое значение нового статуса.";
                return RedirectToAction(nameof(Orders));
            }

            if (prevCanon == nextCanon)
            {
                TempData["ToastType"] = "info";
                TempData["ToastMessage"] = $"Статус заказа #{orderId} уже: «{nextCanon}»";
                return RedirectToAction(nameof(Orders));
            }

            if (!OrderStatusWorkflow.CanTransition(prevCanon, nextCanon, out var fail))
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = fail;
                return RedirectToAction(nameof(Orders));
            }

            try
            {
                await _api.UpdateOrderStatusAsync(orderId, nextCanon);
            }
            catch (Exception ex)
            {
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = $"Не удалось сохранить статус: {ex.Message}";
                return RedirectToAction(nameof(Orders));
            }

            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = $"Статус заказа #{orderId} изменён на «{nextCanon}»";

            try
            {
                string? toEmail = order.GuestEmail;

                if (string.IsNullOrWhiteSpace(toEmail) && order.UserId != null)
                {
                    var users = await _authApi.GetAllUsersAsync();
                    toEmail = users.FirstOrDefault(u => u.Id == order.UserId.Value)?.Email;
                }

                if (!string.IsNullOrWhiteSpace(toEmail))
                {
                    var lines = await BuildOrderLinesForStatusEmailAsync(orderId, order);

                    await _email.SendOrderStatusChangedAsync(
                        toEmail,
                        orderId,
                        prevCanon,
                        nextCanon,
                        order.Address,
                        order.TotalAmount,
                        lines);
                }
            }
            catch
            {
            }

            return RedirectToAction(nameof(Orders));
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
            return AdminShellView(model, "dashboard", "none");
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
            return AdminShellView(model, "reports", "none");
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

        private IActionResult AdminShellView(object model, string panelKey, string searchTarget = "none")
        {
            ViewBag.AdminPanelKey = panelKey;
            ViewBag.AdminSearchTarget = searchTarget;
            var sectionTitle = AdminPageTitleHelper.GetForPanel(panelKey);
            if (!string.IsNullOrEmpty(sectionTitle))
            {
                ViewBag.AdminSectionTitle = sectionTitle;
                ViewData["Title"] = sectionTitle;
            }

            if (AdminPartialHelper.IsPartial(Request))
            {
                Response.Headers["X-Admin-Panel"] = panelKey;
                Response.Headers["X-Admin-Search"] = searchTarget;
                if (!string.IsNullOrEmpty(sectionTitle))
                    Response.Headers["X-Admin-Page-Title"] = AdminPageTitleHelper.EncodeForHeader(sectionTitle);
                return PartialView(model);
            }

            return View(model);
        }

        private async Task<List<OrderLine>> BuildOrderLinesForStatusEmailAsync(int orderId, Order order)
        {
            try
            {
                var details = await _repo.GetOrderEmailDetailsAsync(orderId);
                if (details?.Lines is { Count: > 0 } emailLines)
                {
                    return emailLines.Select(l =>
                    {
                        var name = string.IsNullOrWhiteSpace(l.ProductName) ? "Товар" : l.ProductName.Trim();
                        if (!string.IsNullOrWhiteSpace(l.ArticleNumber))
                            name = $"{name} (арт. {l.ArticleNumber.Trim()})";

                        return new OrderLine
                        {
                            Name = name,
                            Quantity = l.Quantity,
                            Price = l.UnitPricePaid,
                            LineTotal = l.LineTotalPaid
                        };
                    }).ToList();
                }
            }
            catch
            {
                // ниже — fallback
            }

            return (order.Items ?? new List<OrderItem>())
                .Select(i => new OrderLine
                {
                    Name = string.IsNullOrWhiteSpace(i.ProductName)
                        ? (i.ProductId > 0 ? $"Товар №{i.ProductId}" : "Позиция заказа")
                        : i.ProductName.Trim(),
                    Quantity = i.Quantity,
                    Price = i.Price,
                    LineTotal = i.Quantity * i.Price
                })
                .ToList();
        }

        private void ApplyStockQuantityFromForm(Product product)
        {
            if (!Request.HasFormContentType) return;
            var raw = Request.Form["StockQuantity"].ToString();
            if (string.IsNullOrWhiteSpace(raw)) return;
            if (int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock)
                || int.TryParse(raw.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out stock))
            {
                if (stock >= 0)
                    product.StockQuantity = stock;
            }
        }

        private void RemoveProductBindingNoise()
        {
            ModelState.Remove("image");
            ModelState.Remove("ArticleNumber");
            ModelState.Remove("CategoryId");
        }

        private void ValidateProductForAdmin(Product product)
        {
            if (product.CategoryId <= 0
                && (!ModelState.TryGetValue(nameof(product.CategoryId), out var categoryState)
                    || !categoryState.Errors.Any()))
            {
                ModelState.AddModelError(nameof(product.CategoryId), "Выберите категорию или создайте новую.");
            }

            if (string.IsNullOrWhiteSpace(product.Name))
                ModelState.AddModelError(nameof(product.Name), "Укажите название товара.");

            if (string.IsNullOrWhiteSpace(product.ShortDescription))
                ModelState.AddModelError(nameof(product.ShortDescription), "Заполните краткое описание товара.");

            if (string.IsNullOrWhiteSpace(product.FullDescription))
                ModelState.AddModelError(nameof(product.FullDescription), "Заполните полное описание товара.");

            if (product.Price <= 0)
                ModelState.AddModelError(nameof(product.Price), "Цена должна быть больше 0.");

            if (product.StockQuantity <= 0)
                ModelState.AddModelError(nameof(product.StockQuantity), "Количество товара на складе должно быть больше 0.");

            if (product.Configurations != null
                && product.Configurations.Any(c =>
                    (!string.IsNullOrWhiteSpace(c.FeatureName) || c.FeatureId > 0)
                    && string.IsNullOrWhiteSpace(c.FeatureValue)))
            {
                ModelState.AddModelError(nameof(product.Configurations), "Заполните значения характеристик или удалите пустые строки.");
            }
        }

        private void ValidateProductImageForCreate(IFormFile? image)
        {
            if (image == null || image.Length == 0)
            {
                ModelState.AddModelError("image", "Загрузите изображение товара.");
                return;
            }

            if (string.IsNullOrWhiteSpace(image.ContentType)
                || !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("image", "Файл товара должен быть изображением.");
            }
        }

        private static string ToAdminProductError(string raw)
        {
            var message = raw;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                    message = msg.GetString() ?? raw;
            }
            catch (JsonException)
            {
            }

            return message switch
            {
                "Price must be greater than zero" => "Цена должна быть больше 0.",
                "Product name cannot be empty" => "Укажите название товара.",
                "CategoryId must be greater than zero" => "Выберите категорию или создайте новую.",
                "Invalid product ID" => "Некорректный идентификатор товара.",
                "Product not found" => "Товар не найден.",
                _ => string.IsNullOrWhiteSpace(message)
                    ? "Не удалось сохранить товар. Проверьте данные и попробуйте ещё раз."
                    : message
            };
        }
    }
}
