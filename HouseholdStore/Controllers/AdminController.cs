using HouseholdStore.Models;
using HouseholdStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Toolify.ProductService.Models;
using static System.Net.Mime.MediaTypeNames;

namespace HouseholdStore.Controllers
{

    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ProductApiService _api;

        public AdminController(ProductApiService api)
        {
            _api = api;
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
            await _api.UpdateAsync(product);

            if (image != null && image.Length > 0)
            {
                await _api.UploadImageAsync(product.Id, image);
            }

            return RedirectToAction("Index");
        }


        public async Task<IActionResult> Create()
        {
            var categories = await _api.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile? image)
        {
            if (product.CategoryId <= 0)
            {
                ModelState.AddModelError("CategoryId", "Выберите категорию!");
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

        [HttpPost]
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
        {
            await _api.UploadImageAsync(id, file);
            return RedirectToAction("Edit", new { id });
        }

    }
}

