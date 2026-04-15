namespace Toolify.ProductService.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? PromoCodeId { get; set; }
        public string GuestFirstName { get; set; }
        public string GuestLastName { get; set; }
        public string GuestEmail { get; set; }
        public string GuestPhone { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Новый";
        public string Address { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new();
        public string? PromoCode { get; set; }
        public string? DeliveryType { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal DeliveryFee { get; set; }
    }
    public class CheckoutPreviewResult
    {
        public decimal SubtotalAfterProductDiscount { get; set; }
        public decimal DiscountFromCategoryClientPercent { get; set; }
        public decimal GoodsTotalBeforePromo { get; set; }
        public int PromoPercent { get; set; }
        public decimal PromoDiscountAmount { get; set; }
        public decimal GoodsAfterPromo { get; set; }
        public decimal ClientFixedRuleAmount { get; set; }
        public decimal CategoryFixedRuleAmount { get; set; }
        public decimal AppliedFixedDiscountAmount { get; set; }
        public decimal NetGoodsAmount { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal GrandTotal { get; set; }
    }
}
