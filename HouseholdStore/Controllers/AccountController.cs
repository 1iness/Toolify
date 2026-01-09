using HouseholdStore.Models;
using HouseholdStore.Services;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Toolify.ProductService.Data;

namespace HouseholdStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthApiService _auth;
        private readonly ProductRepository _productRepo;

        public AccountController(AuthApiService auth, ProductRepository productRepo)
        {
            _auth = auth;
            _productRepo = productRepo;
        }

        [HttpGet]
        public IActionResult Login()
        {
            var model = new LoginViewModel
            {
                Email = TempData["LoginEmail"] as string,
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var success = await _auth.Register(model);

            if (!success)
            {
                ModelState.AddModelError("", "Пользователь с таким Email уже существует ");
                return View(model);
            }


            return RedirectToAction("ConfirmEmail", new { email = model.Email });
        }

        [HttpGet]
        public IActionResult ConfirmEmail(string email)
        {
            return View(new ConfirmEmailViewModel { Email = email });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var success = await _auth.ConfirmEmail(model.Email, model.Code);

            if (!success)
            {
                ModelState.AddModelError("", "Неверный или просроченный код");
                return View(model);
            }

            TempData["LoginEmail"] = model.Email;

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

            var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "id")?.Value;
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

            var guestId = Request.Cookies["GuestId"];
            if (!string.IsNullOrEmpty(guestId) && !string.IsNullOrEmpty(userIdClaim))
            {
                if (int.TryParse(userIdClaim, out int userId))
                {
                    await _productRepo.MergeCartsAsync(guestId, userId);

                    Response.Cookies.Delete("GuestId");
                }
            }

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

            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                        User.FindFirstValue("id") ??
                        User.FindFirstValue("sub");

            int.TryParse(idStr, out int userId);

            var userOrders = await _productRepo.GetUserOrdersAsync(userId);

            var model = new UserProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Orders = userOrders
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

        [HttpPost]
        public async Task<IActionResult> ResendConfirmCode(string email)
        {
            await _auth.ResendConfirmCode(email);
            return Ok();
        }


    }
}