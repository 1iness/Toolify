using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HouseholdStore.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Введите электронную почту.")]
        [EmailAddress(ErrorMessage = "Некорректный формат email.")]
        [Display(Name = "Электронная почта")] 
        public string Email { get; set; }

        [Required(ErrorMessage = "Введите пароль.")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; }

        [Display(Name = "Запомнить меня")]
        public bool Remember { get; set; }
    }
}
