using System.ComponentModel.DataAnnotations;

namespace HouseholdStore.Models
{
    public class ResetCodeViewModel
    {
        public string Email { get; set; }

        [Required]
        public string Code { get; set; }
    }
}
