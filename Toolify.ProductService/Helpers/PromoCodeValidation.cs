using Toolify.ProductService.Models;

namespace Toolify.ProductService.Helpers;

public static class PromoCodeValidation
{
    public static string? GetRejectReason(PromoCode? promo, string code, decimal? goodsTotalBeforePromo = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var trimmed = code.Trim();

        if (promo == null || !promo.Code.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
            return "Промокод не найден, истёк или исчерпан";

        if (!promo.IsActive)
            return "Промокод отключён";

        var now = DateTime.Now;
        if (now < promo.StartDate || now > promo.EndDate)
            return "Промокод не найден, истёк или исчерпан";

        if (promo.MaxUses.HasValue && promo.UsedCount >= promo.MaxUses.Value)
            return "Промокод исчерпан";

        if (promo.MinGoodsAmount.HasValue
            && goodsTotalBeforePromo.HasValue
            && goodsTotalBeforePromo.Value < promo.MinGoodsAmount.Value)
            return $"Промокод действует от суммы товаров {promo.MinGoodsAmount.Value:N2} BYN";

        return null;
    }

    public static bool IsApplicable(PromoCode? promo, string code, decimal goodsTotalBeforePromo) =>
        GetRejectReason(promo, code, goodsTotalBeforePromo) == null;
}
