namespace Toolify.ProductService;

public static class OrderStatusWorkflow
{
    public static readonly IReadOnlyList<string> MainChain = new[]
    {
        "Новый",
        "В обработке",
        "Доставляется",
        "Доставлен",
        "Завершен"
    };

    public const string Cancelled = "Отменен";

    public static List<string> GetSelectOptions(string? currentStatusRaw)
    {
        if (!TryNormalize(currentStatusRaw, out var cur))
            return new List<string> { string.IsNullOrWhiteSpace(currentStatusRaw) ? "—" : currentStatusRaw.Trim() };

        if (cur == Cancelled || cur == MainChain[^1])
            return new List<string> { cur };

        var opts = new List<string> { cur };

        void Add(string s)
        {
            if (!opts.Contains(s))
                opts.Add(s);
        }

        var ix = IndexInMain(cur);
        if (ix >= 0 && ix < MainChain.Count - 1)
            Add(MainChain[ix + 1]);

        if (CanCancelFrom(cur))
            Add(Cancelled);

        return opts;
    }

    public static bool IsTerminal(string? currentStatusRaw) =>
        TryNormalize(currentStatusRaw, out var c) &&
        (c == Cancelled || c == MainChain[^1]);

    public static bool TryNormalize(string? status, out string canonical)
    {
        canonical = "";
        if (string.IsNullOrWhiteSpace(status))
            return false;

        var t = status.Trim();
        foreach (var s in MainChain)
        {
            if (string.Equals(t, s, StringComparison.OrdinalIgnoreCase))
            {
                canonical = s;
                return true;
            }
        }

        if (string.Equals(t, Cancelled, StringComparison.OrdinalIgnoreCase))
        {
            canonical = Cancelled;
            return true;
        }

        return false;
    }

    public static bool CanTransition(string? fromRaw, string? toRaw, out string failureMessage)
    {
        failureMessage = "";
        if (!TryNormalize(fromRaw, out var from))
        {
            failureMessage =
                "Текущий статус заказа не распознан. Обратитесь к разработчикам или проверьте данные в БД.";
            return false;
        }

        if (!TryNormalize(toRaw, out var to))
        {
            failureMessage = "Недопустимое новое значение статуса.";
            return false;
        }

        if (from == to)
            return true;

        if (from == Cancelled || from == MainChain[^1])
        {
            failureMessage =
                $"Заказ уже в статусе «{from}». Назад к предыдущим статусам перейти нельзя.";
            return false;
        }

        if (to == Cancelled)
        {
            if (CanCancelFrom(from))
                return true;
            failureMessage =
                $"Статус «{from}» нельзя сменить на «{Cancelled}» (разрешено только до доставки).";
            return false;
        }

        var fromIx = IndexInMain(from);
        var toIx = IndexInMain(to);
        if (fromIx < 0 || toIx < 0)
        {
            failureMessage = "Внутренняя ошибка последовательности статусов.";
            return false;
        }

        if (toIx == fromIx + 1)
            return true;

        if (toIx <= fromIx)
        {
            failureMessage =
                $"Нельзя вернуться к предыдущему статусу и нельзя пропускать шаги. Сейчас: «{from}», разрешён следующий только «{MainChain[fromIx + 1]}».";
            return false;
        }

        failureMessage =
            $"Нельзя перескочить несколько статусов. Текущий: «{from}». Выберите следующий по очереди: «{MainChain[fromIx + 1]}».";
        return false;
    }

    private static bool CanCancelFrom(string normalizedMainOrCancelled)
    {
        if (normalizedMainOrCancelled == Cancelled)
            return false;
        return normalizedMainOrCancelled is "Новый" or "В обработке" or "Доставляется";
    }

    private static int IndexInMain(string canonicalMainStatus)
    {
        for (var i = 0; i < MainChain.Count; i++)
        {
            if (MainChain[i] == canonicalMainStatus)
                return i;
        }

        return -1;
    }
}
