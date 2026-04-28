using HouseholdStore.Services;
using Microsoft.AspNetCore.Mvc;
using Toolify.ProductService.Models;

namespace HouseholdStore.ViewComponents
{
    public class NavigationCategoriesViewComponent : ViewComponent
    {
        private readonly ProductApiService _api;

        public NavigationCategoriesViewComponent(ProductApiService api)
        {
            _api = api;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var list = await _api.GetCategoriesAsync();
            return View(list ?? new List<Category>());
        }
    }
}
