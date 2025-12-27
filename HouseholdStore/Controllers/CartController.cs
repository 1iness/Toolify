using Microsoft.AspNetCore.Mvc;

namespace HouseholdStore.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
