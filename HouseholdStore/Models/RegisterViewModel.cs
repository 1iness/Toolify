using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace HouseholdStore.Models
{
    public class RegisterViewModel 
    {
        [Required(ErrorMessage = "Введите имя")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Введите фамилию")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Введите Email")]
        [EmailAddress(ErrorMessage = "Некорректный формат Email")]
        [RegularExpression(
           @"^[^@\s]+@(gmail\.com|mail\.ru)$",
           ErrorMessage = "Допустимы только gmail.com или mail.ru"
       )]
        public string Email { get; set; }

        [Required(ErrorMessage = "Введите номер телефона")]
        [RegularExpression(
            @"^\+375\s?\((25|29|33|44)\)\s?\d{3}-\d{2}-\d{2}$",
            ErrorMessage = "Введите белорусский номер: +375 (29) XXX-XX-XX"
        )]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Введите пароль")]
        [MinLength(8, ErrorMessage = "Минимум 8 символов")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Подтвердите пароль")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }
    }
}
