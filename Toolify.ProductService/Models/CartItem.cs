namespace Toolify.ProductService.Models
{
    public class CartItem 
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }      
        public decimal? OldPrice { get; set; }  
        public int Quantity { get; set; }
        public int StockQuantity { get; set; }
        public string? ArticleNumber { get; set; }
        public bool StockKnown => StockQuantity >= 0;
        public int PurchasableQuantity =>
            !StockKnown ? Quantity : (StockQuantity <= 0 ? 0 : Math.Min(Quantity, StockQuantity));
        public bool IsUnavailable => StockKnown && StockQuantity <= 0;
        public bool HasStockShortage => StockKnown && StockQuantity > 0 && Quantity > StockQuantity;
        public decimal TotalPrice => Price * PurchasableQuantity;
        public decimal? TotalOldPrice => OldPrice * PurchasableQuantity;
    }
}
