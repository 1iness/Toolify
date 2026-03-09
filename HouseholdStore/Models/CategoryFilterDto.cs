namespace HouseholdStore.Models
{
    public class CategoryFilterDto
    {
        public int FeatureId { get; set; }
        public string FeatureName { get; set; } = string.Empty;
        public List<string> AvailableValues { get; set; } = new List<string>();
    }
}
