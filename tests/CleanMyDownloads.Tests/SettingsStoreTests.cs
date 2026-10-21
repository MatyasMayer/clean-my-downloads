using CleanMyDownloads.Core;
using Xunit;

namespace CleanMyDownloads.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    readonly string dir = Path.Combine(Path.GetTempPath(), "cmd-settings-" + Guid.NewGuid().ToString("N"));
    string FilePath => Path.Combine(dir, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    static Settings Ok(SettingsResult r) => Assert.IsType<SettingsResult.Loaded>(r).Settings;
    static string Err(SettingsResult r) => Assert.IsType<SettingsResult.Invalid>(r).Message;

    [Fact]
    public void Missing_file_is_written_with_the_spec_defaults_and_they_are_used()
    {
        var settings = Ok(new SettingsStore(FilePath).Load());

        Assert.True(File.Exists(FilePath));
        Assert.Equal("Pictures", settings.Types.FolderFor("a.png"));
        Assert.Equal("OpenPonk", settings.Types.FolderFor("m.opp"));
        Assert.Contains("task", settings.GenericWords);
        Assert.Contains("cz", settings.LanguageTags);
        Assert.Empty(settings.NotASubject);
        Assert.Equal(TimeSpan.FromHours(3), settings.SettleTime);
        Assert.Equal(TimeSpan.FromHours(24), settings.OppSettleTime);
        Assert.Equal(TimeSpan.FromDays(1), settings.LooseFileWait);
    }

    [Fact]
    public void Written_defaults_round_trip_on_the_next_load()
    {
        var store = new SettingsStore(FilePath);
        var first = Ok(store.Load());
        var second = Ok(store.Load());

        Assert.Equal(first.GenericWords, second.GenericWords);
        Assert.Equal(first.SettleTime, second.SettleTime);
        Assert.Equal("Videos", second.Types.FolderFor("c.mkv"));
    }

    [Fact]
    public void User_edit_adding_an_extension_to_a_folder_takes_effect()
    {
        var store = new SettingsStore(FilePath);
        store.Load();
        var json = File.ReadAllText(FilePath).Replace("\"opp\"", "\"opp\", \"blend\"");
        File.WriteAllText(FilePath, json);

        Assert.Equal("OpenPonk", Ok(store.Load()).Types.FolderFor("scene.blend"));
    }

    [Fact]
    public void User_edit_adding_a_new_folder_and_wait_time_takes_effect()
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(FilePath, """
            {
              "typeFolders": { "Models": ["stl", ".obj"] },
              "genericWords": ["foo"], "languageTags": ["xx"], "notASubject": ["MI-FIT"],
              "settleHours": 5, "oppSettleHours": 30, "looseFileDays": 2
            }
            """);

        var s = Ok(new SettingsStore(FilePath).Load());

        Assert.Equal("Models", s.Types.FolderFor("part.STL"));
        Assert.Equal("Models", s.Types.FolderFor("part.obj"));
        Assert.Equal("Other", s.Types.FolderFor("a.png"));
        Assert.Equal(["MI-FIT"], s.NotASubject);
        Assert.Equal(TimeSpan.FromHours(5), s.SettleTime);
        Assert.Equal(TimeSpan.FromHours(30), s.OppSettleTime);
        Assert.Equal(TimeSpan.FromDays(2), s.LooseFileWait);
    }

    [Fact]
    public void Bad_json_is_invalid_and_the_file_is_left_untouched()
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(FilePath, "{ nope");

        var msg = Err(new SettingsStore(FilePath).Load());

        Assert.Contains("settings.json", msg);
        Assert.Contains("JSON", msg);
        Assert.Equal("{ nope", File.ReadAllText(FilePath));
    }

    [Theory]
    [InlineData("[]", "object")]
    [InlineData("""{"typeFolders": [], "genericWords": [], "languageTags": [], "notASubject": [], "settleHours": 3, "oppSettleHours": 24, "looseFileDays": 1}""", "typeFolders")]
    [InlineData("""{"typeFolders": {"A": "png"}, "genericWords": [], "languageTags": [], "notASubject": [], "settleHours": 3, "oppSettleHours": 24, "looseFileDays": 1}""", "typeFolders")]
    [InlineData("""{"typeFolders": {}, "genericWords": "x", "languageTags": [], "notASubject": [], "settleHours": 3, "oppSettleHours": 24, "looseFileDays": 1}""", "genericWords")]
    [InlineData("""{"typeFolders": {}, "genericWords": [], "languageTags": [], "notASubject": [], "settleHours": "soon", "oppSettleHours": 24, "looseFileDays": 1}""", "settleHours")]
    [InlineData("""{"typeFolders": {}, "genericWords": [], "languageTags": [], "notASubject": [], "settleHours": -1, "oppSettleHours": 24, "looseFileDays": 1}""", "settleHours")]
    [InlineData("""{"typeFolders": {"A": ["png"], "B": ["png"]}, "genericWords": [], "languageTags": [], "notASubject": [], "settleHours": 3, "oppSettleHours": 24, "looseFileDays": 1}""", "png")]
    public void Wrong_shape_is_invalid_and_names_the_problem(string json, string mention)
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(FilePath, json);

        Assert.Contains(mention, Err(new SettingsStore(FilePath).Load()));
    }

    [Fact]
    public void Missing_keys_in_a_valid_file_are_invalid_not_silently_defaulted()
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(FilePath, "{}");

        Assert.Contains("typeFolders", Err(new SettingsStore(FilePath).Load()));
    }
}
