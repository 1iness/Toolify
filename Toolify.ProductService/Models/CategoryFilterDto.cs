namespace Toolify.ProductService.Models
{
    public class CategoryFilterDto
    {
        public int FeatureId { get; set; }
        public string FeatureName { get; set; } = string.Empty;
        public List<string> AvailableValues { get; set; } = new List<string>();
        public List<CategoryFilterValueDto> Values { get; set; } = new List<CategoryFilterValueDto>();
    }

    public class CategoryFilterValueDto
    {
        public int FeatureId { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}
