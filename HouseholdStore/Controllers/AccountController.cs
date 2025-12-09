using HouseholdStore.Models;
using HouseholdStore.Services;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HouseholdStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthApiService _auth;

        public AccountController(AuthApiService auth)
        {
            _auth = auth;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _auth.Register(model);
            if (!result)
            {
                ModelState.AddModelError("", "Registration error");
                return View(model);
            }

            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var token = await _auth.Login(model);

            if (token == null)
            {
                ModelState.AddModelError("", "Invalid login or password");
                return View(model);
            }

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var claims = jwtToken.Claims
                .Select(c => new System.Security.Claims.Claim(c.Type, c.Value))
                .ToList();

            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Cookies");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal);

            Response.Cookies.Append("jwt", token);

            return RedirectToAction("Index", "Home");

        }
    }
}