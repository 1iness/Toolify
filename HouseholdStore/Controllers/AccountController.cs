using Microsoft.AspNetCore.Mvc;
using HouseholdStore.Models;

namespace HouseholdStore.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Email == "admin@mail.com" && model.Password == "12345")
                {
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Неверный логин или пароль");
            }
            return View(model);
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //проверки + сейв в бд

                    TempData["SuccessMessage"] = "Регистрация прошла успешно! Теперь вы можете войти.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Произошла ошибка: {ex.Message}");
                }
            }
            return View(model);
        }
    }
}
