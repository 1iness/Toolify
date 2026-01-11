using System.ComponentModel.DataAnnotations;

namespace HouseholdStore.Models
{
    public class NewPasswordViewModel
    {
        public string Email { get; set; }

        [Required(ErrorMessage = "Введите пароль")]
        [MinLength(8, ErrorMessage = "Пароль должен содержать минимум 8 символов")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Подтвердите пароль")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}
