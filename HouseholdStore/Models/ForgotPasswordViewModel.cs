using System.ComponentModel.DataAnnotations;

namespace HouseholdStore.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Введите Email")]
        [EmailAddress(ErrorMessage = "Введите корректный Email")]
        [RegularExpression(
        @"^[a-zA-Z0-9._%+-]+@gmail\.com$",
        ErrorMessage = "Почта должна иметь такой формат example@gmail.com"
        )]
        public string Email { get; set; }
    }
}
