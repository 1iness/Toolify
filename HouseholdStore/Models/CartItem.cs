using Microsoft.AspNetCore.Mvc;

namespace HouseholdStore.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }      // Текущая цена
        public decimal? OldPrice { get; set; }  // Цена до скидки
        public int Quantity { get; set; }

        // Считаем итоговую сумму для этой позиции
        public decimal TotalPrice => Price * Quantity;
        public decimal? TotalOldPrice => OldPrice * Quantity;
    }
}
