using HouseholdStore.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Toolify.ProductService.Models;

namespace HouseholdStore.Services
{
    public class ProductApiService
    {
        private readonly HttpClient _http;

        public ProductApiService(HttpClient http)
        {
            _http = http;
            _http.BaseAddress = new Uri("https://localhost:7188");
        }

        private JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<List<Product>> GetAllAsync()
        {
            var response = await _http.GetAsync("/api/admin/products");
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"API ERROR: {response.StatusCode} => {err}");
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Product>>(json, jsonOptions);
        }

        public async Task<List<Product>> GetStoreCatalogAsync(int? userId = null)
        {
            var url = "/api/Product";
            if (userId.HasValue) url += $"?userId={userId.Value}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"API ERROR: {response.StatusCode} => {err}");
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Product>>(json, jsonOptions) ?? new List<Product>();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            var response = await _http.GetAsync($"/api/admin/products/{id}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Product>(json, jsonOptions);
        }

        public async Task<Product?> GetStoreProductByIdAsync(int id, int? userId = null)
        {
            var url = $"/api/Product/{id}";
            if (userId.HasValue) url += $"?userId={userId.Value}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Product>(json, jsonOptions);
        }

        public async Task<int?> CreateAsync(Product product)
        {
            var body = JsonSerializer.Serialize(product);
            var response = await _http.PostAsync(
                "/api/admin/products",
                new StringContent(body, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("id").GetInt32();
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            var body = JsonSerializer.Serialize(product);
            var response = await _http.PutAsync(
                $"/api/admin/products/{product.Id}",
                new StringContent(body, Encoding.UTF8, "application/json"));

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _http.DeleteAsync($"/api/admin/products/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UploadImageAsync(int id, IFormFile file)
        {
            using var content = new MultipartFormDataContent();

            var fileStream = file.OpenReadStream();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

            content.Add(fileContent, "file", file.FileName);
            var response = await _http.PostAsync($"/api/Product/{id}/upload-image", content);

            return response.IsSuccessStatusCode;
        }
        public async Task<List<Category>> GetCategoriesAsync()
        {
            var response = await _http.GetAsync("api/Product/categories");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Category>>(jsonOptions);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            var response = await _http.PostAsJsonAsync("api/Product/categories", category);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Category>(jsonOptions);
        }
        public async Task<List<Product>> SearchProductsAsync(string query, int? userId = null)
        {
            var q = Uri.EscapeDataString(query);
            var url = $"api/Product/search?query={q}";
            if (userId.HasValue) url += $"&userId={userId.Value}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<Product>();

            return await response.Content.ReadFromJsonAsync<List<Product>>(jsonOptions) ?? new List<Product>();
        }
        public async Task<List<ReviewViewModel>> GetReviewsAsync(int productId)
        {
            var response = await _http.GetAsync($"/api/reviews/{productId}");
            if (!response.IsSuccessStatusCode) return new List<ReviewViewModel>();

            return await response.Content.ReadFromJsonAsync<List<ReviewViewModel>>(jsonOptions);
        }

        public async Task AddReviewAsync(ReviewViewModel review)
        {
            var response = await _http.PostAsJsonAsync("/api/reviews", review);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<ProductFeature>> GetFeaturesByCategoryAsync(int categoryId)
        {
            var response = await _http.GetAsync($"/api/admin/products/features/{categoryId}");
            if (!response.IsSuccessStatusCode) return new List<ProductFeature>();

            return await response.Content.ReadFromJsonAsync<List<ProductFeature>>(jsonOptions) ?? new List<ProductFeature>();
        }
        public async Task<ProductFeature?> AddFeatureToCategoryAsync(int categoryId, string featureName)
        {
            var content = new StringContent(JsonSerializer.Serialize(new { CategoryId = categoryId, Name = featureName }), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/admin/products/features", content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProductFeature>(jsonOptions);
            }
            return null;
        }
        public async Task UpdateConfigurationsAsync(int productId, List<ProductConfiguration> configurations)
        {
            var response = await _http.PostAsJsonAsync($"/api/Product/{productId}/configurations", configurations);
            response.EnsureSuccessStatusCode();
        }
        public async Task<List<Toolify.ProductService.Models.CategoryFilterDto>> GetCategoryFiltersAsync(int categoryId)
        {
            var response = await _http.GetAsync($"/api/Product/filters/{categoryId}");
            if (!response.IsSuccessStatusCode) return new List<Toolify.ProductService.Models.CategoryFilterDto>();

            return await response.Content.ReadFromJsonAsync<List<Toolify.ProductService.Models.CategoryFilterDto>>(jsonOptions)
                   ?? new List<Toolify.ProductService.Models.CategoryFilterDto>();
        }
        public async Task<List<dynamic>> GetDynamicFiltersAsync(int categoryId)
        {
            var response = await _http.GetAsync($"/api/Product/features/{categoryId}");
            if (!response.IsSuccessStatusCode) return new List<dynamic>();
            return await response.Content.ReadFromJsonAsync<List<dynamic>>(jsonOptions);
        }
        public async Task<List<PromoCode>> GetAllPromoCodesAsync()
        {
            var response = await _http.GetAsync("/api/admin/promocodes");
            if (!response.IsSuccessStatusCode) return new List<PromoCode>();

            return await response.Content.ReadFromJsonAsync<List<PromoCode>>(jsonOptions)
                   ?? new List<PromoCode>();
        }

        public async Task CreatePromoCodeAsync(string code, int discount, DateTime start, DateTime end, int? maxUses = null, decimal? minGoodsAmount = null)
        {
            var data = new { Code = code, DiscountPercent = discount, StartDate = start, EndDate = end, MaxUses = maxUses, MinGoodsAmount = minGoodsAmount };
            await _http.PostAsJsonAsync("/api/admin/promocodes", data);
        }
        public async Task<int?> GetPromoDiscountAsync(string code, decimal? goodsTotal = null)
        {
            var url = $"/api/admin/promocodes/validate/{code}";
            if (goodsTotal.HasValue) url += $"?goodsTotal={goodsTotal.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var data = await response.Content.ReadFromJsonAsync<JsonElement>();
            return data.GetProperty("discountPercent").GetInt32();
        }
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var response = await _http.GetAsync("/api/admin/orders");
            if (!response.IsSuccessStatusCode) return new List<Order>();

            return await response.Content.ReadFromJsonAsync<List<Order>>(jsonOptions) ?? new List<Order>();
        }
        public async Task UpdateOrderStatusAsync(int orderId, string status)
        {
            var content = JsonContent.Create(status);
            var response = await _http.PostAsync($"/api/admin/orders/{orderId}/status", content);

            response.EnsureSuccessStatusCode();
        }
        public async Task AddFavouriteAsync(int userId, int productId)
        {
            await _http.PostAsync(
                $"/api/Product/favourites/add?userId={userId}&productId={productId}", null);
        }

        public async Task RemoveFavouriteAsync(int userId, int productId)
        {
            await _http.PostAsync(
                $"/api/Product/favourites/remove?userId={userId}&productId={productId}", null);
        }

        public async Task<List<Product>> GetFavouritesAsync(int userId)
        {
            var response = await _http.GetAsync($"/api/Product/favourites/{userId}");
            if (!response.IsSuccessStatusCode) return new List<Product>();
            return await response.Content.ReadFromJsonAsync<List<Product>>(jsonOptions)
                   ?? new List<Product>();
        }

        public async Task<bool> IsFavouriteAsync(int userId, int productId)
        {
            var response = await _http.GetAsync(
                $"/api/Product/favourites/check?userId={userId}&productId={productId}");
            if (!response.IsSuccessStatusCode) return false;
            var data = await response.Content.ReadFromJsonAsync<JsonElement>();
            return data.GetProperty("isFavourite").GetBoolean();
        }

        public async Task<List<Promotion>> GetPromotionsAsync()
        {
            var response = await _http.GetAsync("/api/admin/promotions");
            if (!response.IsSuccessStatusCode) return new List<Promotion>();
            return await response.Content.ReadFromJsonAsync<List<Promotion>>(jsonOptions)
                   ?? new List<Promotion>();
        }

        public async Task<(bool ok, string? error)> UpsertPromotionAsync(bool isUpdate, Promotion p)
        {
            var body = JsonSerializer.Serialize(new
            {
                p.Name,
                p.Description,
                p.PromotionType,
                p.ScopeType,
                p.CategoryId,
                p.ProductId,
                p.BuyQty,
                p.PayQty,
                p.PercentOff,
                p.MinOrderAmount,
                p.GiftDescription,
                p.StartDate,
                p.EndDate,
                p.Priority,
                p.IsActive
            });

            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = isUpdate
                ? await _http.PutAsync($"/api/admin/promotions/{p.Id}", content)
                : await _http.PostAsync("/api/admin/promotions", content);

            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool ok, string? error)> DeletePromotionAsync(int id)
        {
            var response = await _http.DeleteAsync($"/api/admin/promotions/{id}");
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<JsonElement> GetPromotionProductStatusAsync(int productId)
        {
            var response = await _http.GetAsync($"/api/admin/promotions/product-status/{productId}");
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"API ERROR: {response.StatusCode} => {err}");
            }
            return await response.Content.ReadFromJsonAsync<JsonElement>(jsonOptions);
        }

        public async Task<List<Discount>> GetDiscountsAsync()
        {
            var response = await _http.GetAsync("/api/admin/discounts");
            if (!response.IsSuccessStatusCode) return new List<Discount>();
            return await response.Content.ReadFromJsonAsync<List<Discount>>(jsonOptions)
                   ?? new List<Discount>();
        }

        public async Task<(bool ok, string? error)> UpsertDiscountAsync(bool isUpdate, Discount d)
        {
            var body = JsonSerializer.Serialize(new
            {
                d.Name,
                d.DiscountType,
                d.ValueKind,
                d.Value,
                d.CategoryId,
                d.ProductId,
                d.MinQuantity,
                d.IsActive
            });

            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = isUpdate
                ? await _http.PutAsync($"/api/admin/discounts/{d.Id}", content)
                : await _http.PostAsync("/api/admin/discounts", content);

            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await response.Content.ReadAsStringAsync());
        }

        public async Task<(bool ok, string? error)> DeleteDiscountAsync(int id)
        {
            var response = await _http.DeleteAsync($"/api/admin/discounts/{id}");
            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, await response.Content.ReadAsStringAsync());
        }
    }
}
