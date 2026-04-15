namespace Toolify.ProductService.Models
{
    public class DiscountRule
    {
        public int Id { get; set; }
        public int? CampaignId { get; set; }
        public string ScopeType { get; set; } = string.Empty;
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        public int? UserId { get; set; }
        public string DiscountMode { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal? MinGoodsAmount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? CategoryName { get; set; }
        public string? ProductName { get; set; }
        public string? CampaignName { get; set; }
    }
}
