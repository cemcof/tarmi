namespace Tarmi.Serializers.Ini;

internal static class Helpers
{
    public static List<string> GetArrayElements(this string value)
    {
        return value
            .Split("], [".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item
                .Replace("]", "", StringComparison.Ordinal)
                .Replace("[", "", StringComparison.Ordinal)
                .Trim()
            )
            .ToList();
    }
}
