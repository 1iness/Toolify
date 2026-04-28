using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HouseholdStore.Helpers;

namespace HouseholdStore.Models
{
    public class NewPasswordViewModel : IValidatableObject
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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(Password) && !PasswordPolicy.MeetsPolicy(Password, out var msg))
                yield return new ValidationResult(msg, new[] { nameof(Password) });
        }
    }
}
