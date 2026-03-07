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
            if (product.Configurations != null && product.Configurations.Any())
            {
                foreach (var config in product.Configurations)
                {
                    if (config.FeatureId == 0 && !string.IsNullOrWhiteSpace(config.FeatureName))
                    {
                        var createdFeature = await _api.AddFeatureToCategoryAsync(product.CategoryId, config.FeatureName);
                        if (createdFeature != null)
                        {
                            config.FeatureId = createdFeature.Id; 
                        }
                    }
                }
            }

            ModelState.Remove("image"); 

            if (ModelState.IsValid)
            {
                await _api.UpdateAsync(product);

                if (product.Configurations != null)
                {
                    await _api.UpdateConfigurationsAsync(product.Id, product.Configurations);
                }

                if (image != null)
                {
                    await _api.UploadImageAsync(product.Id, image);
                }

                return RedirectToAction("Index");
            }

            var categories = await _api.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }


        public async Task<IActionResult> Create()
        {
            var categories = await _api.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(new Product());

        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile? image, string? NewCategoryName)
        {
            var rnd = new Random();
            product.ArticleNumber = rnd.Next(10000, 99999).ToString();

            if (!string.IsNullOrWhiteSpace(NewCategoryName))
            {
                var newCat = new Category { Name = NewCategoryName };
                var createdCat = await _api.CreateCategoryAsync(newCat);
                product.CategoryId = createdCat.Id;
                ModelState.Remove("CategoryId");
            }
            else if (product.CategoryId <= 0)
            {
                ModelState.AddModelError("CategoryId", "Выберите категорию или создайте новую!");
            }

            if (product.CategoryId > 0 && product.Configurations != null && product.Configurations.Any())
            {
                foreach (var config in product.Configurations)
                {
                    if (config.FeatureId == 0 && !string.IsNullOrWhiteSpace(config.FeatureName))
                    {
                        var createdFeature = await _api.AddFeatureToCategoryAsync(product.CategoryId, config.FeatureName);
                        if (createdFeature != null)
                        {
                            config.FeatureId = createdFeature.Id; 
                        }
                    }
                }
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

        [HttpGet("Admin/GetFeatures")] 
        public async Task<IActionResult> GetFeatures(int categoryId)
        {
            var features = await _api.GetFeaturesByCategoryAsync(categoryId);
            return Json(features);
        }
    }
}

