using CleanMyDownloads.Core;

namespace CleanMyDownloads.Sorter;

public static class App
{
    /// <summary>
    /// Usage: --preview [folder] or --sort [folder]. The folder defaults to Downloads.
    /// <paramref name="mover"/> and <paramref name="logPath"/> default to the real disk and the default move log.
    /// Settings are read first; invalid settings skip the whole run (exit code 3) and are reported.
    /// </summary>
    public static int Run(string[] args, IFolderReader folder, IClock clock, IDocumentViewer viewer,
        IFileMover? mover = null, string? logPath = null,
        SettingsStore? settingsStore = null, Action<string>? reportInvalidSettings = null, INotifier? notifier = null)
    {
        if (args.Length == 0 || args[0] is not ("--preview" or "--sort" or "--undo"))
            return 2;
        var loaded = (settingsStore ?? new SettingsStore(SettingsStore.DefaultPath)).Load();
        if (loaded is SettingsResult.Invalid invalid)
        {
            reportInvalidSettings?.Invoke(invalid.Message);
            notifier?.Notify("Clean my downloads: invalid settings", invalid.Message);
            return 3;
        }
        var settings = ((SettingsResult.Loaded)loaded).Settings;
        var path = args.Length > 1
            ? args[1]
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        if (args[0] == "--preview")
        {
            viewer.Show(Preview.Build(folder, path, clock, settings.Types));
            return 0;
        }
        if (args[0] == "--undo")
        {
            UndoRun.Execute(mover ?? new DiskFileMover(), clock, new MoveLog(logPath ?? MoveLog.DefaultPath), path);
            return 0;
        }
        var result = SortRun.Execute(folder, mover ?? new DiskFileMover(), clock, new MoveLog(logPath ?? MoveLog.DefaultPath), path, settings.Types);
        if (result.Moved >= NotifyThreshold)
            notifier?.Notify("Clean my downloads", $"Sorted {result.Moved} files.");
        return 0;
    }

    const int NotifyThreshold = 3;
}
