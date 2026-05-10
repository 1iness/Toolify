namespace Toolify.ProductService.Models
{
    public class OrderHistoryDto
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string? Address { get; set; }
        public string? DeliveryType { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();

        public string GetDeliveryDisplayLine()
        {
            var del = (DeliveryType ?? string.Empty).Trim();
            var addr = (Address ?? string.Empty).Trim();
            if (string.Equals(del, "Pickup", StringComparison.OrdinalIgnoreCase)
                || addr.Equals("Самовывоз", StringComparison.OrdinalIgnoreCase))
                return "Самовывоз";
            return string.IsNullOrEmpty(addr) ? "—" : addr;
        }
    }
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
