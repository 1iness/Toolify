namespace Toolify.ProductService.Models
{
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

        public List<AppliedRule> AppliedRules { get; set; } = new();
    }

    public class AppliedRule
    {
        public string Kind { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
