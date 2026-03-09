namespace HouseholdStore.Models
{
    public class CatalogFilterViewModel
    {
        public int? CategoryId { get; set; }
        public string? SpecialCategory { get; set; }
        public string? Sort { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public Dictionary<int, List<string>> SelectedFeatures { get; set; } = new Dictionary<int, List<string>>();
    }
}