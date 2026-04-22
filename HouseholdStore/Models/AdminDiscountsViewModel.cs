using Toolify.ProductService.Models;

namespace HouseholdStore.Models
{
    public class AdminPromotionsViewModel
    {
        public List<Promotion> Promotions { get; set; } = new();
    }

    public class AdminDiscountsViewModel
    {
        public List<Discount> Discounts { get; set; } = new();
    }
}
