using System.Text;
using System.Text.RegularExpressions;

namespace CAT.Services.Ai;

public static class AiTagMatcher
{
    public static IReadOnlyList<string> FindCandidates(string input, IEnumerable<string?> existingTags, int maxCandidates = 5)
    {
        var normalizedInput = Normalize(input);
        if (normalizedInput.Length == 0)
            return Array.Empty<string>();

        var candidates = existingTags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => new TagCandidate(tag!, Normalize(tag!)))
            .Where(candidate => candidate.Normalized.Length > 0)
            .Where(candidate => !IsDigits(normalizedInput) || normalizedInput == candidate.Normalized)
            .GroupBy(candidate => candidate.Tag, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(candidate => candidate with { Score = Score(normalizedInput, candidate.Normalized) })
            .Where(candidate => candidate.Score >= RequiredScore(normalizedInput, candidate.Normalized))
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Tag.Length)
            .ThenBy(candidate => candidate.Tag, StringComparer.OrdinalIgnoreCase)
            .Take(maxCandidates)
            .Select(candidate => candidate.Tag)
            .ToList();

        if (candidates.Count <= 1)
            return candidates;

        var bestScore = Score(normalizedInput, Normalize(candidates[0]));
        return candidates
            .Where(tag => Math.Abs(Score(normalizedInput, Normalize(tag)) - bestScore) < 0.0001)
            .Take(maxCandidates)
            .ToList();
    }

    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
                builder.Append(Transliterate(ch));
        }

        var normalized = Regex.Replace(builder.ToString(), "([a-z])(?=\\1{2,})", string.Empty, RegexOptions.CultureInvariant);
        normalized = normalized.Replace("tegt", "tag", StringComparison.Ordinal);
        return normalized switch
        {
            "teg" => "tag",
            _ => normalized
        };
    }

    private static double RequiredScore(string input, string candidate)
    {
        if (input == candidate)
            return 1.0;
        if (input.Length <= 3 || candidate.Length <= 3)
            return 1.0;
        if (candidate.Contains(input, StringComparison.Ordinal) || input.Contains(candidate, StringComparison.Ordinal))
            return 0.92;
        return Math.Max(0.78, 1.0 - 1.5 / Math.Max(input.Length, candidate.Length));
    }

    private static double Score(string input, string candidate)
    {
        if (input == candidate)
            return 1.0;
        if (candidate.Contains(input, StringComparison.Ordinal) || input.Contains(candidate, StringComparison.Ordinal))
            return 0.95;

        var distance = Levenshtein(input, candidate);
        return 1.0 - (double)distance / Math.Max(input.Length, candidate.Length);
    }

    private static bool IsDigits(string value)
        => value.All(char.IsDigit);

    private static int Levenshtein(string left, string right)
    {
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var j = 0; j <= right.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private static string Transliterate(char ch)
        => ch switch
        {
            'а' => "a",
            'б' => "b",
            'в' => "v",
            'г' => "g",
            'д' => "d",
            'е' or 'ё' => "e",
            'ж' => "zh",
            'з' => "z",
            'и' or 'й' => "i",
            'к' => "k",
            'л' => "l",
            'м' => "m",
            'н' => "n",
            'о' => "o",
            'п' => "p",
            'р' => "r",
            'с' => "s",
            'т' => "t",
            'у' => "u",
            'ф' => "f",
            'х' => "h",
            'ц' => "c",
            'ч' => "ch",
            'ш' or 'щ' => "sh",
            'ы' => "y",
            'э' => "e",
            'ю' => "yu",
            'я' => "ya",
            _ => ch.ToString()
        };

    private sealed record TagCandidate(string Tag, string Normalized)
    {
        public double Score { get; init; }
    }
}
