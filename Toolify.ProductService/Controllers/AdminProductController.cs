using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toolify.ProductService.Models;
using Toolify.ProductService.Services;

namespace Toolify.ProductService.Controllers
{
    [ApiController]
    [Route("api/admin/products")]
    [Authorize(Roles = "Admin")]
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
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Файл не найден" });

            var product = await _service.GetByIdAsync(id);
            if (product == null)
                return NotFound(new { message = "Товар не найден" });

            string folder = Path.Combine(_env.WebRootPath, "images", "products");
            Directory.CreateDirectory(folder);

            string extension = Path.GetExtension(file.FileName);
            string fileName = $"product_{id}_{Guid.NewGuid()}{extension}";
            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            // удаляем старую
            if (!string.IsNullOrEmpty(product.ImagePath))
            {
                string oldPath = Path.Combine(_env.WebRootPath, product.ImagePath);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            product.ImagePath = $"images/products/{fileName}";
            await _service.UpdateAsync(product);

            return Ok(new { message = "Изображение загружено", path = product.ImagePath });
        }
    }
}
