namespace CAT.Services.Ai;

public static class AiEntityNormalizer
{
    private static readonly System.Text.RegularExpressions.Regex SpokenDigitSequenceRegex = new(
        @"^\s*\d+(?:[\s.,-]+\d+){2,7}\s*\.?\s*$",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly string[] AnimalTagPrefixes =
    [
        "cow",
        "bull",
        "calf",
        "heifer",
        "animal",
        "корова",
        "бык",
        "телка",
        "тёлка",
        "теленок",
        "телёнок",
        "животное"
    ];

    public static string? NormalizeAnimalTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return tag;

        var value = tag.Trim();
        foreach (var prefix in AnimalTagPrefixes)
        {
            var snakePrefix = prefix + "_";
            if (value.StartsWith(snakePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var candidate = value[snakePrefix.Length..].Trim();
                if (IsPlainNumber(candidate)) return candidate;
            }

            var spacePrefix = prefix + " ";
            if (value.StartsWith(spacePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var candidate = value[spacePrefix.Length..].Trim();
                if (IsPlainNumber(candidate)) return candidate;
            }
        }

        if (SpokenDigitSequenceRegex.IsMatch(value))
        {
            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (!string.IsNullOrWhiteSpace(digits))
                return digits;
        }

        return value;
    }

    private static bool IsPlainNumber(string value)
        => value.Length > 0 && value.All(char.IsDigit);
}
