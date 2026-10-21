using CleanMyDownloads.Core;
using CleanMyDownloads.Sorter;

return App.Run(args, new DiskFolderReader(), new SystemClock(), new NotepadViewer(),
    new SettingsStore(SettingsStore.DefaultPath), Console.Error.WriteLine);
