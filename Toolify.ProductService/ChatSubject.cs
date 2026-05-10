namespace Toolify.ProductService;

public static class ChatSubject
{
    public const string LegacyPlaceholder = "Вопрос по сайту";

    public static string Resolve(string? explicitSubject, string messageText)
    {
        var ex = explicitSubject?.Trim();
        if (!string.IsNullOrWhiteSpace(ex))
            return ex.Length > 100 ? Truncate(ex, 97) : ex;

        var msg = messageText?.Trim();
        if (string.IsNullOrWhiteSpace(msg))
            return "Обращение в поддержку";

        var rawLine = msg.Split(["\r\n", "\n", "\r"], StringSplitOptions.None)
                .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))
            ?? msg;
        var line = rawLine.Trim();
        return line.Length > 72 ? Truncate(line, 69) : line;

        static string Truncate(string s, int maxChars) => s[..maxChars].TrimEnd() + "…";
    }

    public static bool IsLegacyPlaceholder(string? subject) =>
        string.IsNullOrWhiteSpace(subject) ||
        LegacyPlaceholder.Equals(subject.Trim(), StringComparison.OrdinalIgnoreCase);

    public static string TitleForAdminList(string? subject, string? lastPreview, int conversationId, int maxLen = 72)
    {
        if (!IsLegacyPlaceholder(subject))
        {
            var t = subject!.Trim();
            return t.Length > maxLen ? TruncateEllipsis(t, maxLen) : t;
        }

        var p = (lastPreview ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(p))
        {
            var oneLine = p.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))
                    ?.Trim()
                ?? p.Trim();
            return oneLine.Length > maxLen ? TruncateEllipsis(oneLine, maxLen) : oneLine;
        }

        return $"Диалог #{conversationId}";
    }

    private static string TruncateEllipsis(string s, int maxLen)
    {
        if (s.Length <= maxLen) return s;
        return s[..maxLen].TrimEnd() + "…";
    }
}
