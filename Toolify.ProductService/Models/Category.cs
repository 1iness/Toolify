
namespace Toolify.ProductService.Models
{
    public class Category 
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? IconFileName { get; set; }
        public List<ProductFeature> Features { get; set; } = new();
    }
}
