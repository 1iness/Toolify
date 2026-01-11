using System.ComponentModel.DataAnnotations;

namespace HouseholdStore.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
