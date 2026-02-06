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
        public int Discount { get; set; }
        public string? ArticleNumber { get; set; }
        public double AverageRating { get; set; }
        public int ReviewsCount { get; set; }
    }
}
