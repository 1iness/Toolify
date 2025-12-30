using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HouseholdStore.Helpers
{
    public class CartHelper 
    {
        public static (int? userId, string? guestId) GetCartIdentifiers(HttpContext context)
        {
            int? userId = null;
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier) ?? context.User.FindFirst("id");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
            {
                userId = id;
            }

            string? guestId = context.Request.Cookies["GuestId"];

            if (userId == null && string.IsNullOrEmpty(guestId))
            {
                guestId = Guid.NewGuid().ToString();
                context.Response.Cookies.Append("GuestId", guestId, new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(30), 
                    HttpOnly = true,
                    IsEssential = true
                });
            }

            return (userId, guestId);
        }
    }
}
