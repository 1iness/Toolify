using HouseholdStore.Models;
using HouseholdStore.Services;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

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

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            var claims = new List<Claim>(jwt.Claims);

            if (!string.IsNullOrEmpty(emailClaim) && !claims.Any(c => c.Type == ClaimTypes.Name))
            {
                claims.Add(new Claim(ClaimTypes.Name, emailClaim));
            }
            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimTypes.Name,
                ClaimTypes.Role
            );

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true, 
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                }
            );

            Response.Cookies.Append("jwt", token);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userEmail = User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            var user = await _auth.GetUserByEmailAsync(userEmail);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var model = new UserProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone
            };

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("jwt");
            return RedirectToAction("Index", "Home"); 
        }
    }
}