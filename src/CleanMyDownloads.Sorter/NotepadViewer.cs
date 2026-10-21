using System.Diagnostics;

namespace CleanMyDownloads.Sorter;

/// <summary>Writes the text to a temp file and opens it in Notepad.</summary>
public sealed class NotepadViewer : IDocumentViewer
{
    public void Show(string text)
    {
        var file = Path.Combine(Path.GetTempPath(), "clean-my-downloads-preview.txt");
        File.WriteAllText(file, text);
        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{file}\"") { UseShellExecute = true });
    }
}
