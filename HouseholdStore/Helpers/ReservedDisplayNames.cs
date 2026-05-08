using System.Text.RegularExpressions;

namespace HouseholdStore.Helpers;

public static class ReservedDisplayNames
{
    public const string FirstNameError =
        "В имени нельзя использовать служебное слово (например, admin или system).";

    public const string LastNameError =
        "В фамилии нельзя использовать служебное слово (например, admin или system).";

    private static readonly Regex SegmentSplitter = new(@"[\s'-]+", RegexOptions.Compiled);

    private static readonly HashSet<string> ReservedTokens =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "admin",
            "administrator",
            "admins",
            "sysadmin",
            "moderator",
            "root",
            "system",
            "daemon",
            "postgresql",
            "postgres",
            "mongodb",
            "oracle",
            "apache",
            "nginx",
            "localhost",
            "guest",
            "anonymous",
            "webmaster",
            "hostmaster",
            "dba",
            "админ",
            "администратор",
            "модератор",
            "система",
            "гость",
            "аноним"
        };

    public static bool ContainsReservedToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        foreach (var seg in SegmentSplitter.Split(value.Trim()))
        {
            if (seg.Length == 0)
                continue;
            if (ReservedTokens.Contains(seg))
                return true;
        }

        return false;
    }
}
