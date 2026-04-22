namespace Toolify.ProductService.Models
{
    public static class PromotionTypes
    {
        public const string BuyGetY = "Купи-получи";
        public const string OrderPercent = "Скидка на заказ";
        public const string FreeShipping = "Бесплатная доставка";
        public const string Gift = "Подарок";
    }

    public static class PromotionScopes
    {
        public const string All = "Все";
        public const string Category = "Категория";
        public const string Product = "Товар";
    }

    public class Promotion
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string PromotionType { get; set; } = string.Empty;
        public string ScopeType { get; set; } = string.Empty;

        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        public int? ProductId { get; set; }
        public string? ProductName { get; set; }

        public int? BuyQty { get; set; }
        public int? PayQty { get; set; }

        public decimal? PercentOff { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public string? GiftDescription { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
