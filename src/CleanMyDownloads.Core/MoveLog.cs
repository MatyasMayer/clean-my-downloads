using System.Text;

namespace CleanMyDownloads.Core;

/// <summary>
/// Append-only, human-readable move log. Line format (two spaces between fields, UTF-8):
/// <code>
/// yyyy-MM-dd HH:mm  RUN START &lt;runId&gt;
/// yyyy-MM-dd HH:mm  &lt;source&gt; → &lt;destination&gt;                  (a move)
/// yyyy-MM-dd HH:mm  DUPLICATE  &lt;source&gt;  (same content as &lt;existing&gt;)
/// yyyy-MM-dd HH:mm  SKIPPED  &lt;source&gt;  (&lt;reason&gt;)
/// yyyy-MM-dd HH:mm  FAILED  &lt;source&gt;  (&lt;reason&gt;)
/// yyyy-MM-dd HH:mm  RUN END &lt;runId&gt;
/// </code>
/// Paths are shown relative to the sorted folder's parent, e.g. <c>Downloads\a.pdf</c>.
/// Every line of a run lies between its START and END markers; an unfinished run has no END.
/// </summary>
public sealed class MoveLog(string path)
{
    public const string TimeFormat = "yyyy-MM-dd HH:mm";
    public const string Arrow = " → ";

    public string Path { get; } = path;

    public void RunStart(DateTime at, string runId) => Append(at, $"RUN START {runId}");
    public void RunEnd(DateTime at, string runId) => Append(at, $"RUN END {runId}");
    public void Moved(DateTime at, string from, string to) => Append(at, from + Arrow + to);
    public void Duplicate(DateTime at, string file, string existing) => Append(at, $"DUPLICATE  {file}  (same content as {existing})");
    public void Skipped(DateTime at, string file, string reason) => Append(at, $"SKIPPED  {file}  ({Flat(reason)})");
    public void Failed(DateTime at, string file, string reason) => Append(at, $"FAILED  {file}  ({Flat(reason)})");

    static string Flat(string s) => s.ReplaceLineEndings(" ");

    void Append(DateTime at, string text)
    {
        var dir = System.IO.Path.GetDirectoryName(Path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.AppendAllText(Path, $"{at.ToString(TimeFormat)}  {text}\r\n", new UTF8Encoding(false));
    }

    public static string DefaultPath { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "clean-my-downloads", "move.log");
}

public enum MoveLogKind { Moved, Duplicate, Skipped, Failed }

/// <param name="Source">Path as logged.</param>
/// <param name="Destination">Only for Moved.</param>
/// <param name="Detail">Reason (Skipped, Failed) or the existing file (Duplicate).</param>
public sealed record MoveLogEntry(DateTime Time, MoveLogKind Kind, string Source, string? Destination, string? Detail);

public sealed record MoveLogRun(string RunId, bool Ended, IReadOnlyList<MoveLogEntry> Entries);

/// <summary>Reads the move log back into runs, for undo and re-sorting.</summary>
public static class MoveLogParser
{
    public static IReadOnlyList<MoveLogRun> Parse(IEnumerable<string> lines)
    {
        var runs = new List<MoveLogRun>();
        string? id = null;
        var entries = new List<MoveLogEntry>();
        var ended = false;

        void Flush()
        {
            if (id != null) runs.Add(new MoveLogRun(id, ended, entries));
            entries = [];
            ended = false;
        }

        foreach (var line in lines)
        {
            if (line.Length < MoveLog.TimeFormat.Length + 2) continue;
            if (!DateTime.TryParseExact(line[..MoveLog.TimeFormat.Length], MoveLog.TimeFormat,
                    null, System.Globalization.DateTimeStyles.None, out var time)) continue;
            var body = line[(MoveLog.TimeFormat.Length + 2)..];

            if (body.StartsWith("RUN START ")) { Flush(); id = body["RUN START ".Length..]; continue; }
            if (body.StartsWith("RUN END ")) { ended = true; continue; }
            if (id == null) continue;

            var entry = ParseEntry(time, body);
            if (entry != null) entries.Add(entry);
        }
        Flush();
        return runs;
    }

    static MoveLogEntry? ParseEntry(DateTime time, string body)
    {
        foreach (var (prefix, kind) in new[]
                 { ("DUPLICATE  ", MoveLogKind.Duplicate), ("SKIPPED  ", MoveLogKind.Skipped), ("FAILED  ", MoveLogKind.Failed) })
        {
            if (!body.StartsWith(prefix)) continue;
            var rest = body[prefix.Length..];
            var split = rest.IndexOf("  (", StringComparison.Ordinal);
            if (split < 0 || !rest.EndsWith(')')) return null;
            var detail = rest[(split + 3)..^1];
            if (kind == MoveLogKind.Duplicate) detail = detail.Replace("same content as ", "");
            return new MoveLogEntry(time, kind, rest[..split], null, detail);
        }
        var arrow = body.IndexOf(MoveLog.Arrow, StringComparison.Ordinal);
        return arrow < 0 ? null
            : new MoveLogEntry(time, MoveLogKind.Moved, body[..arrow], body[(arrow + MoveLog.Arrow.Length)..], null);
    }
}
