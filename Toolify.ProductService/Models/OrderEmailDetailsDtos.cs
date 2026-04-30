namespace Toolify.ProductService.Models
{
    public class OrderEmailDetails
    {
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DeliveryFee { get; set; }
        public string DeliveryType { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string GuestPhone { get; set; } = string.Empty;
        public string GuestEmail { get; set; } = string.Empty;
        public string? PromoCode { get; set; }
        public int PromoDiscountPercent { get; set; }
        public decimal GoodsTotal { get; set; }
    }

    public class OrderEmailLine
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ArticleNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPricePaid { get; set; }
        public decimal UnitListPrice { get; set; }
        public decimal LineTotalPaid { get; set; }
        public decimal LineTotalList { get; set; }
    }

    public class OrderEmailPayload
    {
        public OrderEmailDetails Header { get; set; } = null!;
        public List<OrderEmailLine> Lines { get; set; } = new();
    }
}
