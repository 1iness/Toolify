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
        public int StockQuantity { get; set; }
        public bool StockKnown => StockQuantity >= 0;
        public int PurchasableQuantity =>
            !StockKnown ? Quantity : (StockQuantity <= 0 ? 0 : Math.Min(Quantity, StockQuantity));

        public decimal TotalPrice => Price * PurchasableQuantity;
        public decimal? TotalOldPrice => OldPrice * PurchasableQuantity;
    }
}
