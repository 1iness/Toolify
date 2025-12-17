using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toolify.ProductService.Models;
using Toolify.ProductService.Services;

namespace Toolify.ProductService.Controllers
{
    [ApiController]
    [Route("api/admin/products")]
    public class AdminProductController : ControllerBase
    {
        private readonly ProductManager _service;
        private readonly IWebHostEnvironment _env;

        public AdminProductController(ProductManager service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
        }

        // GET: api/admin/products
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _service.GetAllAsync();
            return Ok(products);
        }

        // GET: api/admin/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _service.GetByIdAsync(id);
            if (product == null)
                return NotFound(new { message = "Товар не найден" });

            return Ok(product);
        }

        // POST: api/admin/products
        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            try
            {
                int id = await _service.AddAsync(product);
                return CreatedAtAction(nameof(GetById), new { id }, new { message = "Товар создан", id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/admin/products/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Product product)
        {
            if (id != product.Id)
                return BadRequest(new { message = "Id товара не совпадает" });

            try
            {
                bool updated = await _service.UpdateAsync(product);
                if (!updated)
                    return NotFound(new { message = "Товар не найден" });

                return Ok(new { message = "Товар обновлён" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/admin/products/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            bool result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = "Товар не найден" });

            return Ok(new { message = "Товар удалён" });
        }

        // POST: upload image
        [HttpPost("{id}/upload-image")]
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Файл не найден");

            var product = await _service.GetByIdAsync(id);
            if (product == null) return NotFound();

            var rootPath = _env.WebRootPath;
            if (string.IsNullOrEmpty(rootPath))
            {
                rootPath = Path.Combine(_env.ContentRootPath, "wwwroot");
            }
            var folder = Path.Combine(rootPath, "images", "products");

            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            product.ImagePath = "/images/products/" + fileName;
            await _service.UpdateAsync(product);

            return Ok(new { path = product.ImagePath });
        }

    }
}
