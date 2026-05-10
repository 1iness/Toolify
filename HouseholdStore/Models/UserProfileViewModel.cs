using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HouseholdStore.Helpers;
using Toolify.ProductService.Models;

namespace HouseholdStore.Models
{
    public class UserProfileViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Введите имя")]
        [StringLength(40, MinimumLength = 2, ErrorMessage = "Имя должно содержать от 2 до 40 символов")]
        [RegularExpression(
            @"^[A-Za-zА-Яа-яЁё]+(?:[ '-][A-Za-zА-Яа-яЁё]+)*$",
            ErrorMessage = "Имя: только буквы, пробел, - и '"
        )]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Введите фамилию")]
        [StringLength(40, MinimumLength = 2, ErrorMessage = "Фамилия должна содержать от 2 до 40 символов")]
        [RegularExpression(
            @"^[A-Za-zА-Яа-яЁё]+(?:[ '-][A-Za-zА-Яа-яЁё]+)*$",
            ErrorMessage = "Фамилия: только буквы, пробел, - и '"
        )]
        public string LastName { get; set; }
        public string? Email { get; set; }
        public string Phone { get; set; }
        public List<OrderHistoryDto> Orders { get; set; } = new();
        public List<PromoCode> AvailablePromoCodes { get; set; } = new();
        public List<Promotion> AvailablePromotions { get; set; } = new();
        public List<Discount> AvailableDiscounts { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ReservedDisplayNames.ContainsReservedToken(FirstName))
                yield return new ValidationResult(ReservedDisplayNames.FirstNameError, new[] { nameof(FirstName) });

            if (ReservedDisplayNames.ContainsReservedToken(LastName))
                yield return new ValidationResult(ReservedDisplayNames.LastNameError, new[] { nameof(LastName) });
        }
    }
}
