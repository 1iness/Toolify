using Toolify.ProductService.Data;
using Toolify.ProductService.Models;

namespace Toolify.ProductService.Services
{
    public class ProductManager 
    {
        private readonly ProductRepository _repository;

        public ProductManager(ProductRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Product>> GetAllAsync()
        {
            var products = await _repository.GetAllAsync();
            return products
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .ToList();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be greater than zero");

            return await _repository.GetByIdAsync(id);
        }
        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _repository.GetAllCategoriesAsync();
        }
        public async Task<int> AddAsync(Product product)
        {
            ValidateProduct(product, isNew: true);
            return await _repository.AddAsync(product);
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            if (product.Id <= 0)
                throw new ArgumentException("Invalid product ID");

            ValidateProduct(product, isNew: false);

            var exists = await _repository.GetByIdAsync(product.Id);
            if (exists == null)
                throw new Exception("Product not found");

            return await _repository.UpdateAsync(product);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid ID");

            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> SetCatalogVisibilityAsync(int id, bool isHiddenFromCatalog)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid ID");

            return await _repository.SetCatalogVisibilityAsync(id, isHiddenFromCatalog);
        }

        private void ValidateProduct(Product product, bool isNew)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (string.IsNullOrWhiteSpace(product.Name))
                throw new ArgumentException("Укажите название товара.");

            if (product.CategoryId <= 0)
                throw new ArgumentException("Выберите категорию или создайте новую.");

            if (product.Price <= 0)
                throw new ArgumentException("Цена должна быть больше 0.");

            if (product.StockQuantity <= 0)
                throw new ArgumentException("Количество товара на складе должно быть больше 0.");

            if (string.IsNullOrWhiteSpace(product.ShortDescription))
                throw new ArgumentException("Заполните краткое описание товара.");

            if (string.IsNullOrWhiteSpace(product.FullDescription))
                throw new ArgumentException("Заполните полное описание товара.");
        }
        public async Task<List<ProductFeature>> GetFeaturesByCategoryAsync(int categoryId)
        {
            return await _repository.GetFeaturesByCategoryAsync(categoryId);
        }
        public async Task<ProductFeature> AddFeatureAsync(int categoryId, string name, bool isTemplate = true)
        {
            return await _repository.AddFeatureAsync(categoryId, name, isTemplate);
        }
        public async Task UpdateConfigurationsAsync(int productId, List<ProductConfiguration> configs)
        {
            await _repository.UpdateProductConfigurationsAsync(productId, configs);
        }
        public async Task<List<CategoryFilterDto>> GetCategoryFiltersAsync(int categoryId)
        {
            return await _repository.GetCategoryFiltersAsync(categoryId);
        }
    }   
}
