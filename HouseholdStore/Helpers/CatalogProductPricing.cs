using Toolify.ProductService.Models;

namespace HouseholdStore.Helpers
{
    public static class CatalogProductPricing
    {
        public static decimal FinalUnitPrice(Product p)
        {
            if (p.CatalogSalePrice.HasValue)
                return p.CatalogSalePrice.Value;
            return p.Price;
        }

        public static bool ShowDiscountStyle(Product p) => p.CatalogCompareAtPrice.HasValue;

        public static int BadgePercent(Product p) =>
            p.CatalogDiscountBadgePercent ?? 0;
    }
}
