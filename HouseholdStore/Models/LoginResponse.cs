using Microsoft.AspNetCore.Mvc;

namespace HouseholdStore.Models
{
    public class LoginResponse 
    {
        public string Token { get; set; } = null!;
    }
}
