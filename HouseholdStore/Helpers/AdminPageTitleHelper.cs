namespace HouseholdStore.Helpers;

public static class AdminPageTitleHelper
{
    private static readonly Dictionary<string, string> ByPanelKey =
        new(StringComparer.Ordinal)
        {
            ["home"] = "Панель управления",
            ["products-list"] = "Список товаров",
            ["products-categories"] = "Категории",
            ["products-create"] = "Добавить товар",
            ["products-edit"] = "Редактировать товар",
            ["promocodes"] = "Управление промокодами",
            ["promotions"] = "Акции",
            ["discounts"] = "Скидки",
            ["clients"] = "Управление учетными записями",
            ["orders"] = "Управление заказами",
            ["dashboard"] = "Общая статистика",
            ["reports"] = "Отчеты",
            ["chat"] = "Чаты с клиентами",
        };

    public static string? GetForPanel(string panelKey)
        => ByPanelKey.TryGetValue(panelKey, out var t) ? t : null;

    public static string EncodeForHeader(string title)
        => Uri.EscapeDataString(title);
}
