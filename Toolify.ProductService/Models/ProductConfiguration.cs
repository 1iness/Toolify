namespace Toolify.ProductService.Models
{
    public class ProductConfiguration
    {
        public int ProductId { get; set; }
        public int FeatureId { get; set; }
        public string FeatureName { get; set; } = string.Empty;
        public string FeatureValue { get; set; } = string.Empty; 
    }
}