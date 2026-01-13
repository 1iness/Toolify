namespace Toolify.Shared
{
    public class OrderEmailItemDto
    {
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = "";
    }

    public interface IOrderDataProvider
    {
        Task<List<OrderEmailItemDto>> GetOrderItemsForEmailAsync(int orderId);
        Task<string?> GetOrderEmailAsync(int orderId);
    }
}
