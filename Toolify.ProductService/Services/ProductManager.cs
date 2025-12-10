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
            return await _repository.GetAllAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be greater than zero");

            return await _repository.GetByIdAsync(id);
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

        private void ValidateProduct(Product product, bool isNew)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (string.IsNullOrWhiteSpace(product.Name))
                throw new ArgumentException("Product name cannot be empty");

            if (product.CategoryId <= 0)
                throw new ArgumentException("CategoryId must be greater than zero");

            if (product.Price <= 0)
                throw new ArgumentException("Price must be greater than zero");
        }    
    }   
}
