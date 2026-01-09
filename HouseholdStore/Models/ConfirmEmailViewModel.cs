using System.ComponentModel.DataAnnotations;

namespace HouseholdStore.Models
{
    public class ConfirmEmailViewModel
    {
        [Required]
        public string Email { get; set; }

        [Required(ErrorMessage = "Введите код")]
        public string Code { get; set; }
    }
}
