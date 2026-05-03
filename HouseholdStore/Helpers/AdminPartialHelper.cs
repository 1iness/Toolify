namespace HouseholdStore.Helpers;

public static class AdminPartialHelper
{

    public static bool IsPartial(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Admin-Partial", out var headerVals))
        {
            foreach (var raw in headerVals)
            {
                if (string.Equals(raw?.Trim(), "1", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        var qp = request.Query["adminPartial"].ToString();
        if (string.IsNullOrEmpty(qp))
            qp = request.Query["partial"].ToString();

        if (!string.Equals(qp.Trim(), "1", StringComparison.OrdinalIgnoreCase))
            return false;

        var mode = request.Headers["Sec-Fetch-Mode"].ToString();
        return string.Equals(mode, "cors", StringComparison.OrdinalIgnoreCase)
               || string.Equals(mode, "same-origin", StringComparison.OrdinalIgnoreCase);
    }
}
