using Microsoft.AspNetCore.Mvc;
using Toolify.ProductService.Data;
using Toolify.ProductService.Models;

namespace Toolify.ProductService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : Controller
    {
        private readonly ProductRepository _repo;

        public ProductController(ProductRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Ok(new List<Product>());

            var products = await _repo.SearchAsync(query);
            return Ok(products);
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _repo.GetAllCategoriesAsync();
            return Ok(categories);
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
                return BadRequest("Имя категории не может быть пустым");

            var newCategory = await _repo.AddCategoryAsync(category);
            return Ok(newCategory);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _repo.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> Add(Product product)
        {
            int newId = await _repo.AddAsync(product);
            return Ok(new { id = newId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Product product)
        {
            product.Id = id;

            bool updated = await _repo.UpdateAsync(product);
            if (!updated) return NotFound();

            return Ok("Updated");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            bool deleted = await _repo.DeleteAsync(id);
            if (!deleted) return NotFound();

            return Ok("Deleted");
        }

        [HttpPost("{id}/upload-image")]
        [Consumes("multipart/form-data")] 
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не получен");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var productImage = new ProductImage
            {
                ProductId = id,
                ImageData = ms.ToArray(),
                ContentType = file.ContentType,
                IsMain = true
            };

            await _repo.AddProductImageAsync(productImage);
            return Ok();
        }

        [HttpGet("{id}/image")]
        public async Task<IActionResult> GetImage(int id)
        {
            var image = await _repo.GetMainImageAsync(id);
            if (image == null) return NotFound();
            return File(image.Value.Data, image.Value.ContentType);
        }

        [HttpGet("features/{categoryId}")]
        public async Task<IActionResult> GetFeatures(int categoryId)
        {
            var features = await _repo.GetFeaturesByCategoryAsync(categoryId);
            return Ok(features);
        }

        [HttpPost("{id}/configurations")]
        public async Task<IActionResult> UpdateConfigs(int id, [FromBody] List<ProductConfiguration> configurations)
        {
            await _repo.UpdateProductConfigurationsAsync(id, configurations); 
            return Ok();
        }
        [HttpGet("filters/{categoryId}")]
        public async Task<IActionResult> GetCategoryFilters(int categoryId)
        {
            var filters = await _repo.GetCategoryFiltersAsync(categoryId); 
            return Ok(filters);
        }


        [HttpPost("favourites/add")]
        public async Task<IActionResult> AddFavourite([FromQuery] int userId, [FromQuery] int productId)
        {
            await _repo.AddFavouriteAsync(userId, productId);
            return Ok();
        }

        [HttpPost("favourites/remove")]
        public async Task<IActionResult> RemoveFavourite([FromQuery] int userId, [FromQuery] int productId)
        {
            await _repo.RemoveFavouriteAsync(userId, productId);
            return Ok();
        }

        [HttpGet("favourites/{userId}")]
        public async Task<IActionResult> GetFavourites(int userId)
        {
            var products = await _repo.GetFavouritesAsync(userId);
            return Ok(products);
        }

        [HttpGet("favourites/check")]
        public async Task<IActionResult> IsFavourite([FromQuery] int userId, [FromQuery] int productId)
        {
            var result = await _repo.IsFavouriteAsync(userId, productId);
            return Ok(new { isFavourite = result });
        }
    }
}
