using System.Text.Json.Serialization;

namespace Toolify.ProductService.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string? ShortDescription { get; set; }
        public string? FullDescription { get; set; }
        public decimal Price { get; set; }
        public string? ImagePath { get; set; }
        public int StockQuantity { get; set; }
        public string? ArticleNumber { get; set; }
        public double AverageRating { get; set; }
        public int ReviewsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ProductImage> Images { get; set; } = new();
        public List<ProductConfiguration> Configurations { get; set; } = new();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? CatalogSalePrice { get; set; }

        //Зачёркнутая цена
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? CatalogCompareAtPrice { get; set; }

        //Процент для бейджа % от полной цены товара.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? CatalogDiscountBadgePercent { get; set; }
    }
}
