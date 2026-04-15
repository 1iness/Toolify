using Toolify.ProductService.Models;

namespace HouseholdStore.Helpers
{
    public static class CatalogProductPricing
    {
        public static decimal FinalUnitPrice(Product p)
        {
            if (p.CatalogSalePrice.HasValue)
                return p.CatalogSalePrice.Value;
            var d = Math.Clamp(p.Discount, 0, 100);
            return p.Price * (100 - d) / 100m;
        }

        public static bool ShowDiscountStyle(Product p) => p.CatalogCompareAtPrice.HasValue;

        public static int BadgePercent(Product p) =>
            p.CatalogDiscountBadgePercent ?? (p.Discount > 0 ? p.Discount : 0);
    }
}
