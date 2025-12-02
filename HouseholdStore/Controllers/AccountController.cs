using Microsoft.AspNetCore.Mvc;

namespace HouseholdStore.Controllers
{
    public class AccountController:Controller
    {
        public IActionResult Index()
        {
            return View();// Страница "Вход/Регистрация"
        }
        public IActionResult Login()
        {
            return View();//Страница входа
        }
        public IActionResult Register()
        {
            return View();//Страница регистрации
        }
    }
}
