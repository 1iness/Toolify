using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toolify.ProductService.Models;
using Toolify.ProductService.Services;

namespace HouseholdStore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ProductManager _service;

        public AdminController(ProductManager service)
        {
            _service = service;
        }

        // Главная панель админа
        public IActionResult Index()
        {
            return View();
        }

        // Список товаров
        public async Task<IActionResult> List()
        {
            var products = await _service.GetAllAsync();
            return View(products);
        }

        // Форма добавления
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile image)
        {
            if (!ModelState.IsValid)
                return View(product);

            int id = await _service.AddAsync(product);

            // загрузка изображения
            if (image != null && image.Length > 0)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                Directory.CreateDirectory(folder);

                string fileName = $"product_{id}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await image.CopyToAsync(stream);

                product.Id = id;
                product.ImagePath = $"images/products/{fileName}";
                await _service.UpdateAsync(product);
            }

            return RedirectToAction("List");
        }

        // Форма редактирования
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _service.GetByIdAsync(id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product product, IFormFile image)
        {
            if (!ModelState.IsValid)
                return View(product);

            if (image != null && image.Length > 0)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                Directory.CreateDirectory(folder);

                string fileName = $"product_{product.Id}_{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await image.CopyToAsync(stream);

                if (!string.IsNullOrEmpty(product.ImagePath))
                {
                    string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImagePath);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                product.ImagePath = $"images/products/{fileName}";
            }

            await _service.UpdateAsync(product);
            return RedirectToAction("List");
        }

        // Удаление
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return RedirectToAction("List");
        }
    }
}
