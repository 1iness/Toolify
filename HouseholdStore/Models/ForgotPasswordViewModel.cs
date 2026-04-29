using System.ComponentModel.DataAnnotations;

namespace HouseholdStore.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Введите Email")]
        [EmailAddress(ErrorMessage = "Введите корректный Email")]
        [MaxLength(254, ErrorMessage = "Email слишком длинный")]
        public string Email { get; set; }
    }
}
