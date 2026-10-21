using System.Text;
using System.Text.RegularExpressions;

namespace CleanMyDownloads.Core;

/// <summary>SPEC section 3.2: file name to stem (the words that name a topic), and when two stems are the same topic.</summary>
public sealed partial class TopicRules
{
    public const int MinStemChars = 3;

    [GeneratedRegex(@"^(\d+[\p{L}]{0,2}|v\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex NumberIsh();

    readonly HashSet<string> generic;
    readonly HashSet<string> languageTags;

    public TopicRules(IEnumerable<string> genericWords, IEnumerable<string> languageTags)
    {
        generic = genericWords.Select(Normalize).ToHashSet(StringComparer.Ordinal);
        this.languageTags = languageTags.Select(Normalize).ToHashSet(StringComparer.Ordinal);
    }

    public static TopicRules Default { get; } = new(Settings.DefaultGenericWords, Settings.DefaultLanguageTags);

    static string Normalize(string s) => s.Trim().ToLowerInvariant();

    /// <summary>The stem as lower-case words, or null when the name has no usable stem.</summary>
    public IReadOnlyList<string>? StemOf(string fileName)
    {
        var kept = new List<string>();
        foreach (var word in Words(Path.GetFileNameWithoutExtension(fileName)))
        {
            if (languageTags.Contains(word)) continue;
            if (NumberIsh().IsMatch(word) || generic.Contains(word)) break;
            kept.Add(word);
        }
        return string.Join('-', kept).Length < MinStemChars ? null : kept;
    }

    /// <summary>Lower-case words of a name, split at separators and lower-to-upper CamelCase boundaries.</summary>
    internal static List<string> Words(string name)
    {
        var sb = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsLower(name[i - 1]) && char.IsUpper(name[i])) sb.Append(' ');
            sb.Append(name[i]);
        }
        return sb.ToString().ToLowerInvariant()
            .Split([' ', '\t', '-', '_', '.', '(', ')', '[', ']', ',', '+'], StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    /// <summary>One stem, ignoring separators, equals the other's first n words joined.</summary>
    public static bool SameTopic(IReadOnlyList<string> a, IReadOnlyList<string> b) =>
        StartsAtWordBoundary(a, string.Concat(b)) || StartsAtWordBoundary(b, string.Concat(a));

    /// <summary>True when <paramref name="flat"/> equals some first-n-words join of <paramref name="words"/>.</summary>
    static bool StartsAtWordBoundary(IReadOnlyList<string> words, string flat)
    {
        var joined = "";
        foreach (var w in words)
        {
            joined += w;
            if (joined == flat) return true;
            if (joined.Length > flat.Length) return false;
        }
        return false;
    }
}
