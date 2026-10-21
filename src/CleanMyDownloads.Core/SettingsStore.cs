using System.Text.Json;

namespace CleanMyDownloads.Core;

/// <summary>Reads (and, if missing, creates) settings.json. The location is injectable.</summary>
public sealed class SettingsStore(string filePath)
{
    public static string DefaultPath { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "clean-my-downloads", "settings.json");

    public string FilePath { get; } = filePath;

    public SettingsResult Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(FilePath))!);
                File.WriteAllText(FilePath, DefaultJson());
            }
            return Parse(File.ReadAllText(FilePath));
        }
        catch (IOException e) { return Fail(e.Message); }
        catch (UnauthorizedAccessException e) { return Fail(e.Message); }
    }

    SettingsResult Fail(string problem) => new SettingsResult.Invalid($"{FilePath}: {problem}");

    static string DefaultJson()
    {
        var o = new Dictionary<string, object>
        {
            ["typeFolders"] = Settings.DefaultTypeFolders,
            ["genericWords"] = Settings.DefaultGenericWords,
            ["languageTags"] = Settings.DefaultLanguageTags,
            ["notASubject"] = Array.Empty<string>(),
            ["settleHours"] = 3,
            ["oppSettleHours"] = 24,
            ["looseFileDays"] = 1,
        };
        return JsonSerializer.Serialize(o, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
    }

    SettingsResult Parse(string text)
    {
        JsonDocument doc;
        try { doc = JsonDocument.Parse(text); }
        catch (JsonException e) { return Fail($"not valid JSON ({e.Message})"); }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return Fail("the top level must be a JSON object");

            if (!Require(root, "typeFolders", JsonValueKind.Object, out var typeFolders, out var err))
                return Fail(err);
            var folders = new Dictionary<string, string[]>();
            var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in typeFolders.EnumerateObject())
            {
                if (!ReadStrings(p.Value, out var exts))
                    return Fail($"typeFolders.{p.Name} must be a list of extensions");
                exts = exts.Select(e => e.TrimStart('.')).ToArray();
                foreach (var e in exts)
                {
                    if (e.Length == 0)
                        return Fail($"typeFolders.{p.Name} contains an empty extension");
                    if (seen.TryGetValue(e, out var other))
                        return Fail($"extension \"{e}\" is listed in both {other} and {p.Name}");
                    seen[e] = p.Name;
                }
                folders[p.Name] = exts;
            }

            var lists = new List<string[]>();
            foreach (var key in new[] { "genericWords", "languageTags", "notASubject" })
            {
                if (!Require(root, key, JsonValueKind.Array, out var el, out err))
                    return Fail(err);
                if (!ReadStrings(el, out var items))
                    return Fail($"{key} must be a list of strings");
                lists.Add(items);
            }

            var spans = new List<TimeSpan>();
            foreach (var (key, unit) in new[] { ("settleHours", 1.0), ("oppSettleHours", 1.0), ("looseFileDays", 24.0) })
            {
                if (!Require(root, key, JsonValueKind.Number, out var el, out err))
                    return Fail(err);
                var n = el.GetDouble();
                if (n < 0 || double.IsInfinity(n) || n > 100_000)
                    return Fail($"{key} must be a non-negative number of {(unit == 1.0 ? "hours" : "days")}");
                spans.Add(TimeSpan.FromHours(n * unit));
            }

            return new SettingsResult.Loaded(new Settings(
                new TypeTable(folders), lists[0], lists[1], lists[2], spans[0], spans[1], spans[2]));
        }
    }

    static bool Require(JsonElement root, string key, JsonValueKind kind, out JsonElement value, out string error)
    {
        error = "";
        if (!root.TryGetProperty(key, out value)) { error = $"\"{key}\" is missing"; return false; }
        if (value.ValueKind != kind)
        {
            error = $"\"{key}\" must be a {kind.ToString().ToLowerInvariant()}";
            return false;
        }
        return true;
    }

    static bool ReadStrings(JsonElement el, out string[] items)
    {
        items = [];
        if (el.ValueKind != JsonValueKind.Array) return false;
        var list = new List<string>();
        foreach (var x in el.EnumerateArray())
        {
            if (x.ValueKind != JsonValueKind.String) return false;
            list.Add(x.GetString()!);
        }
        items = list.ToArray();
        return true;
    }
}
