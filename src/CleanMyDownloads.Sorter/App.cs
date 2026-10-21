using CleanMyDownloads.Core;

namespace CleanMyDownloads.Sorter;

public static class App
{
    /// <summary>
    /// Usage: --preview, --sort or --undo, each with an optional folder (defaults to Downloads), or
    /// --init-settings, which only writes the default settings if missing (used by the installer).
    /// <paramref name="mover"/> and <paramref name="logPath"/> default to the real disk and the default move log.
    /// Preview and sort read settings first; invalid settings skip the whole run (exit code 3) and are logged,
    /// reported and notified. Undo does not need settings.
    /// </summary>
    public static int Run(string[] args, IFolderReader folder, IClock clock, IDocumentViewer viewer,
        IFileMover? mover = null, string? logPath = null,
        SettingsStore? settingsStore = null, Action<string>? reportInvalidSettings = null, INotifier? notifier = null)
    {
        if (args.Length == 0 || args[0] is not ("--preview" or "--sort" or "--undo" or "--init-settings"))
            return 2;
        var path = args.Length > 1
            ? args[1]
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var log = new MoveLog(logPath ?? MoveLog.DefaultPath);

        if (args[0] == "--undo")
        {
            var undone = UndoRun.Execute(mover ?? new DiskFileMover(), clock, log, path);
            if (undone.SkippedFiles.Count > 0)
                viewer.Show("Undo could not restore these files, because they were moved or deleted since the run:\r\n"
                    + string.Join("\r\n", undone.SkippedFiles) + "\r\n");
            return 0;
        }

        var store = settingsStore ?? new SettingsStore(SettingsStore.DefaultPath);
        var loaded = store.Load();
        if (loaded is SettingsResult.Invalid invalid)
        {
            var runId = Guid.NewGuid().ToString("N")[..8];
            log.RunStart(clock.Now, runId);
            log.Failed(clock.Now, store.FilePath, "invalid settings, run skipped: " + invalid.Message);
            log.RunEnd(clock.Now, runId);
            reportInvalidSettings?.Invoke(invalid.Message);
            notifier?.Notify("Clean my downloads: invalid settings", invalid.Message);
            return 3;
        }
        if (args[0] == "--init-settings")
            return 0;
        var plan = PlanOptions.From(((SettingsResult.Loaded)loaded).Settings);
        if (args[0] == "--preview")
        {
            viewer.Show(Preview.Build(folder, path, clock, plan: plan, logPath: log.Path));
            return 0;
        }
        var result = SortRun.Execute(folder, mover ?? new DiskFileMover(), clock, log, path, plan: plan);
        if (result.Moved >= NotifyThreshold)
            notifier?.Notify("Clean my downloads", $"Sorted {result.Moved} files.");
        return 0;
    }

    const int NotifyThreshold = 3;
}
