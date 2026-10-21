using System.Text.RegularExpressions;

namespace CleanMyDownloads.Core;

/// <summary>SPEC section 3.1: file name to subject folder name (e.g. <c>BI-AG1</c>), or null.</summary>
public sealed partial class SubjectClassifier
{
    public const string SchoolFolder = "School";

    [GeneratedRegex(
        @"(?<![a-z0-9])(anie|ani|qnie|qni|bie|bik|bi|nie|ni|mie|mi)[-_ ]([a-z][a-z0-9]{2}[0-9]?|[0-9][a-z]{2})(?:\.([0-9]{1,2}))?(?![a-z0-9])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();

    static readonly Dictionary<string, string> PrefixFolder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BIE"] = "BI", ["BIK"] = "BI", ["NIE"] = "NI", ["MIE"] = "MI", ["ANIE"] = "ANI", ["QNIE"] = "QNI",
    };

    readonly HashSet<string> notASubject;

    public SubjectClassifier(IEnumerable<string>? notASubject = null) =>
        this.notASubject = new HashSet<string>(
            (notASubject ?? []).Select(c => c.Trim().ToUpperInvariant()), StringComparer.Ordinal);

    public static SubjectClassifier Default { get; } = new();

    /// <summary>The subject folder name for a file name, or null when it has no subject code.</summary>
    public string? FolderFor(string fileName)
    {
        foreach (Match m in CodePattern().Matches(fileName))
        {
            var prefix = m.Groups[1].Value.ToUpperInvariant();
            prefix = PrefixFolder.GetValueOrDefault(prefix, prefix);
            var code = prefix + "-" + m.Groups[2].Value.ToUpperInvariant();
            if (!notASubject.Contains(code)) return code;
        }
        return null;
    }

    /// <summary>Destination folder relative to the sorted folder, or null when not a subject.</summary>
    public string? DestinationFor(string fileName) =>
        FolderFor(fileName) is { } code ? Path.Combine(SchoolFolder, code) : null;
}
