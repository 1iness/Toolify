namespace Toolify.ProductService.Models
{
    public static class DiscountTypes
    {
        public const string Quantity = "Количество";
        public const string Product = "Товар";
        public const string Category = "Категория";
    }

    public static class DiscountValueKinds
    {
        public const string Percent = "Процент";
        public const string Fixed = "Сумма";
    }

    public class Discount
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string DiscountType { get; set; } = string.Empty;
        public string ValueKind { get; set; } = string.Empty;
        public decimal Value { get; set; }

        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }

        public int? ProductId { get; set; }
        public string? ProductName { get; set; }

        public int? MinQuantity { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
