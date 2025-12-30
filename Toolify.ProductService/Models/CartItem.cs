using Toolify.ProductService.Models;

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
        public decimal TotalPrice => Price * Quantity;
        public decimal? TotalOldPrice => OldPrice * Quantity;
    }
}
