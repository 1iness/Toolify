using Toolify.ProductService.Models;
using System.ComponentModel.DataAnnotations;

namespace HouseholdStore.Models
{
    public class CheckoutViewModel 
    {
        public List<Toolify.ProductService.Models.CartItem> CartItems { get; set; } = new List<Toolify.ProductService.Models.CartItem>();

        public decimal TotalAmount => CartItems.Sum(x => x.TotalPrice);

        [Required(ErrorMessage = "Введите имя")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Введите фамилию")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Введите Email")]
        [EmailAddress(ErrorMessage = "Некорректный Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Введите телефон")]
        [RegularExpression(@"^\+375\s\(\d{2}\)\s\d{3}-\d{2}-\d{2}$", ErrorMessage = "Формат: +375 (XX) XXX-XX-XX")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Введите адрес доставки")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Введите номер карты")]
        public string CardNumber { get; set; }

        [Required(ErrorMessage = "Введите срок действия")]
        public string CardExpiry { get; set; }

        [Required(ErrorMessage = "Введите CVV")]
        public string CardCvv { get; set; }
    }
}
