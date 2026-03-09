namespace Toolify.ProductService.Models
{
    public class ProductFeature
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty; 
    }
}