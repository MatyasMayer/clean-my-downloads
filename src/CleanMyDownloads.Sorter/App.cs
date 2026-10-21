using CleanMyDownloads.Core;

namespace CleanMyDownloads.Sorter;

public static class App
{
    /// <summary>Usage: --preview [folder]. The folder defaults to Downloads.</summary>
    public static int Run(string[] args, IFolderReader folder, IClock clock, IDocumentViewer viewer,
        SettingsStore? settingsStore = null, Action<string>? reportInvalidSettings = null)
    {
        if (args.Length == 0 || args[0] != "--preview")
            return 2;
        var loaded = (settingsStore ?? new SettingsStore(SettingsStore.DefaultPath)).Load();
        if (loaded is SettingsResult.Invalid invalid)
        {
            reportInvalidSettings?.Invoke(invalid.Message);
            return 3;
        }
        var settings = ((SettingsResult.Loaded)loaded).Settings;
        var path = args.Length > 1
            ? args[1]
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        viewer.Show(Preview.Build(folder, path, clock, settings.Types));
        return 0;
    }
}
