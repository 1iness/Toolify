using HouseholdStore.Models;
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

        public async Task<Product?> GetByIdAsync(int id)
        {
            var response = await _http.GetAsync($"/api/admin/products/{id}");
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

        public async Task<string?> UploadImageAsync(int id, IFormFile file)
        {
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            content.Add(fileContent, "file", file.FileName);

            var response = await _http.PostAsync($"/api/admin/products/{id}/upload-image", content);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("path").GetString();
        }
        public async Task<List<Category>> GetCategoriesAsync()
        {
            var response = await _http.GetAsync("/api/categories");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<Category>>();
        }
    }
}
