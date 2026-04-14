using Toolify.ProductService.Models;

namespace HouseholdStore.Models
{
    public class AdminDiscountsViewModel
    {
        public List<DiscountCampaign> Campaigns { get; set; } = new();
        public List<DiscountRule> Rules { get; set; } = new();
    }
}
